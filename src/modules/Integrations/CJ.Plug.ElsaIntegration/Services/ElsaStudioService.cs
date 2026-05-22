using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Studio.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Elsa.Studio;
using Elsa.Studio.Workflows.Domain.Extensions;
using IActivityRegistry = Elsa.Studio.Workflows.Domain.Contracts.IActivityRegistry;
using Serilog;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Workflows.Runtime;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using ExecuteWorkflowResult = Elsa.Studio.Workflows.Domain.Models.ExecuteWorkflowResult;
using WorkflowDefinition = Elsa.Api.Client.Resources.WorkflowDefinitions.Models.WorkflowDefinition;
using IWorkflowDefinitionService = Elsa.Studio.Workflows.Domain.Contracts.IWorkflowDefinitionService;
using Elsa.Api.Client.Resources.WorkflowInstances.Requests;
using CJ.Plug.ElsaIntegration.Contracts;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using CJ.Plug.Models.Job;
using System.Text.Json;
using CJ.Plug.Models.LogModels;
using CJ.Plug.PlugDataZoneApiClient;
using CJ.Plug.JobManageApiClient;
using Elsa.Studio.Login;




namespace CJ.Plug.ElsaIntegration.Services
{
    public class ElsaStudioService : IElsaStudioService
    {
        private IWorkflowDefinitionEditorService WorkflowDefinitionEditorService { get; set; }
        private IWorkflowDefinitionService WorkflowDefinitionService { get; set; }
        
        private ICredentialsValidator CredentialsValidator { get; set; }
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; }
        private IJwtAccessor JwtAccessor { get; set; }
        private IActivityRegistry ActivityRegistry { get; set; }
        private IBackendApiClientProvider backendApiClientProvider { get; set; }
        private IWorkflowInstanceService WorkflowInstanceService { get; set; }
        private IActivityExecutionService ActivityExecutionService { get; set; }
        private MainApiClient MainApiClient { get; set; }
        public ElsaStudioService(
            IWorkflowDefinitionEditorService workflowDefinitionEditorService,
            IWorkflowDefinitionService workflowDefinitionService,
            ICredentialsValidator credentialsValidator,
            AuthenticationStateProvider authenticationStateProvider,
            IJwtAccessor jwtAccessor,
            IActivityRegistry activityRegistry,
            IBackendApiClientProvider _backendApiClientProvider,
            IWorkflowInstanceService workflowInstanceService,
            IActivityExecutionService activityExecutionService,
            MainApiClient mainApiClient
            )
        {
            WorkflowDefinitionEditorService = workflowDefinitionEditorService;
            WorkflowDefinitionService = workflowDefinitionService;
            CredentialsValidator = credentialsValidator;
            AuthenticationStateProvider = authenticationStateProvider;
            JwtAccessor = jwtAccessor;
            ActivityRegistry= activityRegistry;
            backendApiClientProvider = _backendApiClientProvider;
            WorkflowInstanceService = workflowInstanceService;
            ActivityExecutionService = activityExecutionService;
            MainApiClient = mainApiClient;


            //在构造函数中登录到引擎，无需每次调用时都登录
            //LoginToElsaEngine();
        }

        public async Task<WorkflowDefinition> CreateOrUpdateWorkflowFromProcess(CJ.Plug.Models.Plug.Plug Plug)
        {
            //创建一个空的引擎流程，用于发布到组件库展示
            var workflowDefinition = new WorkflowDefinition()
            {
                DefinitionId = Plug.DefinitionId,
                Name = Plug.Name,                
                Version = 1,
                IsLatest = true,
                //Description = Plug.CoreType,
                //Outcomes = ["11","22","33"],                
                Root = Plug.ToActivityJson()   
            };
            //workflowDefinition.Root.SetCanStartWorkflow(true);
            workflowDefinition.Options.UsableAsActivity = Plug.ShowInPlugLibrary;
            workflowDefinition.Options.ActivityCategory = Plug.GroupName;
            workflowDefinition.Options.AutoUpdateConsumingWorkflows = false;
            //先取消发布，防止有历史版本正处于发布中
            //await WorkflowDefinitionEditorService.RetractAsync(workflowDefinition);
            //或者直接删除引擎活动重新创建
            await DeleteWorkflowByDefinitionId(workflowDefinition.DefinitionId);
            await WorkflowDefinitionEditorService.SaveAsync(workflowDefinition, true);
            return workflowDefinition;
        }

        public async Task<bool> DeleteWorkflowByDefinitionId(string WorkflowDefinitionId)
        {
            return await WorkflowDefinitionService.DeleteAsync(WorkflowDefinitionId);
        }

        public async Task<WorkflowDefinition?> FindWorkflowDefinitionByPlug(CJ.Plug.Models.Plug.Plug Plug)
        {
            //var result=await WorkflowDefinitionService.FindByDefinitionIdAsync(Plug.ProcessDefinitionId,VersionOptions.Latest);
            var result=await FindWorkflowDefinitionByDefinitionId(Plug.ParentPlugDefinitionId);
            if (result == null)
            {
                Console.WriteLine("create new workflow");
                var workflow = new WorkflowDefinition()
                {
                    Name = Plug.Name,
                    DefinitionId = Plug.DefinitionId,
                    Root = Plug.ToActivityJson()
                };
                await SaveAsync(workflow,false);
                
            }
            return result;
        }

        public async Task<WorkflowDefinition?> FindWorkflowDefinitionByDefinitionId(string WorkflowDefinitionId)
        {
            //var result = await WorkflowDefinitionService.FindWorkflowDefinitionAsync(WorkflowDefinitionId,Elsa.Common.Models.VersionOptions.Latest);
            var result = await WorkflowDefinitionService.FindByDefinitionIdAsync(WorkflowDefinitionId,VersionOptions.Latest);
            return result;
        }

        public async Task<ExecuteWorkflowResult> ExecuteWithOutWaitAsync(CJ.Plug.Models.Plug.Plug Plug, CancellationToken cancellationToken = default)
        {
            //需要通过流程定义创建流程再执行，目前引擎无法支持直接通过json执行包含自定义活动的流程

            //Log.Information("测试直接调用引擎服务执行JSON流程");

            //var workflowJson = new JsonObject(new Dictionary<string, JsonNode?>
            //{
            //    ["id"] = Plug.DefinitionId,
            //    ["definitionId"] = Plug.DefinitionId,
            //    ["name"] = Plug.Name,
            //    ["root"] = Plug.ToActivityJson()
            //}).ToString();
            //Console.WriteLine(workflowJson);
            //// Deserialize the workflow model.
            //var workflowDefinitionModel = ActivitySerializer.Deserialize<Elsa.Workflows.Management.Models.WorkflowDefinitionModel>(workflowJson);
            //Log.Information("workflowDefinitionModel:"+JsonSerializer.Serialize(workflowDefinitionModel));

            //// Map the model to a Workflow object.
            //var workflow = WorkflowDefinitionMapper.Map(workflowDefinitionModel);
            ////var workflow = WorkflowDefinitionMapper.Map(workflowDefinition);
            //Console.WriteLine(JsonSerializer.Serialize("workflow:"+workflow));
            //// Run the workflow.
            ////自定义的活动目前引擎不支持直接通过JSON运行
            //var result2 = await WorkflowRunner.RunAsync(workflow);
            ////Log.Information(JsonSerializer.Serialize(result2));
            //Log.Information(result2.WorkflowState.Status.ToString());
            //Log.Information(result2.Workflow?.Root?.ToString());
            //Log.Information(result2.Result?.ToString());
            await CreateOrUpdateWorkflowFromProcess(Plug);
            var request = new ExecuteWorkflowDefinitionRequest
            {
                //使用自定义的ID进行流程实例化跟踪，后续如果引擎支持自定义ID则可以去掉
                CorrelationId = RandomLongIdentityGenerator.GenerateId(),
            };

            Log.Information("execute workflow 1");
            //var api = await GetExecuteWorkflowApiAsync(cancellationToken);
            var api = await backendApiClientProvider.GetApiAsync<IExecuteWorkflowApi>(cancellationToken);
            // 直接调用异步方法，不使用 await 等待其完成
            var responseTask = api.ExecuteAsync(Plug.DefinitionId, request, cancellationToken).ConfigureAwait(false);
            // 获取响应（注意：此时响应可能还未完成）
            //HttpResponseMessage response = responseTask.GetAwaiter().GetResult();
            Log.Information(request.CorrelationId);
            return new ExecuteWorkflowResult(request.CorrelationId, false);
            
        }


        public async Task<ExecuteWorkflowResult> ExecuteAsync(CJ.Plug.Models.Plug.Plug Plug,string? userName,CancellationToken cancellationToken = default)
        {
            //这里是作业方式执行，需要将原始PDZ复制一份为新的作业PDZ用于承载作业数据，未完待续
            //var CorrelationId = RandomLongIdentityGenerator.GenerateId();
            //1 通过Plug获取原始PDZ
            var rootPDZ = await MainApiClient.GetOrCreatePDZFromPlug(Plug, userName);
            //2 复制原始PDZ为新的作业PDZ
            var newPDZ=await MainApiClient.CreateJobPDZByCopyPDZ(rootPDZ,userName);
            if(newPDZ == null)
            {
                CLog.Error($"创建作业PDZ失败");
                return null;
            }

            var executeResult = await ExecuteWithCorrelationIdAsync(Plug, newPDZ.PDZId);

            return executeResult;
        }

        public async Task<ExecuteWorkflowResult> ExecuteWithCorrelationIdAsync(CJ.Plug.Models.Plug.Plug Plug,string CorrelationId, CancellationToken cancellationToken = default)
        {
            try
            {
                await CreateOrUpdateWorkflowFromProcess(Plug);

                //创建一条Job记录，用于后续作业追踪
                var job = new ProcessJob
                {
                    //EngineInstanceId = executeResult.WorkflowInstanceId,
                    JobCorrelationId = CorrelationId,
                    ProcessDefinitionId = Plug.DefinitionId,
                    CreatedAt = DateTimeOffset.UtcNow.ToLocalTime(),
                    UpdatedAt = DateTimeOffset.UtcNow.ToLocalTime()
                };
                job = await MainApiClient.CreateJobAsync(job);
                //用CorrelationId创建一个PDZ，用于后续的执行数据承载
                await MainApiClient.GetOrCreateJobPDZ(CorrelationId);


                var request = new ExecuteWorkflowDefinitionRequest
                {
                    //使用自定义的ID进行流程实例化跟踪，后续如果引擎支持自定义ID则可以去掉
                    CorrelationId = CorrelationId,
                    VersionOptions = VersionOptions.Latest,
                };
                var result = await WorkflowDefinitionService.ExecuteAsync(Plug.DefinitionId, request);
                //更新实例ID至Job记录
                job.EngineInstanceId = result.WorkflowInstanceId;
                await MainApiClient.UpdateJobAsync(job);
                //Log.Information("Job updated:" + JsonSerializer.Serialize(job));


                return result;

                //var api = await backendApiClientProvider.GetApiAsync<IExecuteWorkflowApi>(cancellationToken);
                //// 直接调用异步方法，不使用 await 等待其完成
                //var responseTask = api.ExecuteAsync(Plug.DefinitionId, request, cancellationToken).ConfigureAwait(false);
                //// 获取响应（注意：此时响应可能还未完成）
                //Log.Information(request.CorrelationId);
                //return new ExecuteWorkflowResult(request.CorrelationId, false);
            }
            catch (Exception ex)
            {
                CLog.Error("ExecuteWithCorrelationIdAsync:"+ex.Message);
                return new ExecuteWorkflowResult(null, true);
            }

        }


        public async Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            var result = await WorkflowDefinitionEditorService.SaveAsync(workflowDefinition, publish, null);
            return result;
        }

        

        public async Task LoginToElsaEngine()
        {
            var authState = await ((AccessTokenAuthenticationStateProvider)AuthenticationStateProvider).GetAuthenticationStateAsync();
            if (authState?.User?.Identity?.IsAuthenticated ?? false)
            {
                Console.WriteLine("already login");
                return;
            }

            var result = await CredentialsValidator.ValidateCredentialsAsync("admin", "password");
            if (!result.IsAuthenticated)
                return;

            await JwtAccessor.WriteTokenAsync(TokenNames.AccessToken, result.AccessToken!);
            await JwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, result.RefreshToken!);

            ((AccessTokenAuthenticationStateProvider)AuthenticationStateProvider).NotifyAuthenticationStateChanged();

            Console.WriteLine("login to elsa success");
        }

        public async Task<IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor>> GetElsaActivityDescriptors()
        {
            await ActivityRegistry.EnsureLoadedAsync();
            return ActivityRegistry.ListBrowsable();
        }

        public async Task<Elsa.Api.Client.Resources.WorkflowInstances.Models.WorkflowInstanceSummary?> GetWorkflowInstanceByCorrelationId(string CorrelationId)
        {
            var instanceList = await WorkflowInstanceService.ListAsync(new());
            var instance = instanceList.Items.FirstOrDefault(x => x.CorrelationId == CorrelationId);
            return instance;
        }

        public async Task<Elsa.Api.Client.Resources.WorkflowInstances.Models.WorkflowInstanceSummary?> GetWorkflowInstanceByInstanceId(string InstanceId)
        {
            var instanceList = await WorkflowInstanceService.ListAsync(new());
            var instance = instanceList.Items.FirstOrDefault(x => x.Id == InstanceId);
            return instance;
        }

        public async Task<List<WorkflowExecutionLogRecord>> GetJournalAsync(string instanceId, JournalFilter? filter = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default)
        {
            var response = await WorkflowInstanceService.GetJournalAsync(instanceId, filter, skip, take);
            return response.Items.ToList();
        }

        public async Task<List<ActivityExecutionRecordSummary>> GetActivityExecutionRecords(string workflowInstanceId, string activityNodeId)
        {
            var records = await ActivityExecutionService.ListSummariesAsync(workflowInstanceId, activityNodeId);
            return records.ToList();
        }

    }


}
