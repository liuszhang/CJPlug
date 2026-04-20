using CJ.Plug.ElsaIntegration.Contracts;
using CJ.Plug.Models.PlugProcess;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Humanizer;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.ElsaIntegration.Services
{
    public class ElsaDomToolService:IElsaDomToolService
    {
        private IDomAccessor DomAccessor { get; set; }
        private IFiles Files { get; set; }
        private IElsaStudioService ElsaStudioService { get; set; }
        private IWorkflowDefinitionEditorService WorkflowDefinitionEditorService { get; set; }

        private IWorkflowDefinitionImporter WorkflowDefinitionImporter { get; set; }

        public ElsaDomToolService(
            IDomAccessor domAccessor, 
            IFiles files, 
            IElsaStudioService elsaStudioService, 
            IWorkflowDefinitionEditorService workflowDefinitionEditorService,
            IWorkflowDefinitionImporter workflowDefinitionImporter)
        {
            DomAccessor = domAccessor;
            Files = files;
            ElsaStudioService = elsaStudioService;
            WorkflowDefinitionEditorService = workflowDefinitionEditorService;
            WorkflowDefinitionImporter = workflowDefinitionImporter;
        }

        public async Task DownloadFileFromStreamAsync(string fileName, Stream stream)
        {
            //调用Elsa框架成熟的文件下载服务
            await Files.DownloadFileFromStreamAsync(fileName, stream);
        }

        public async Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default)
        {
            DomAccessor.ClickElementAsync(elementRef);
        }

        public async Task ExportAsync(Plug.Models.Plug.Plug Process, CancellationToken cancellationToken = default)
        {
            //var workflowDefinition = await WorkflowDefinitionService.FindByDefinitionIdAsync(workflowDefinitionDefinitionId, VersionOptions.Latest);
            var workflowDefinition = await ElsaStudioService.CreateOrUpdateWorkflowFromProcess(Process);
            var download = await WorkflowDefinitionEditorService.ExportAsync(workflowDefinition!);
            var fileName = string.IsNullOrEmpty(workflowDefinition!.Name) ? $"{workflowDefinition.DefinitionId}.json" : $"{workflowDefinition!.Name.Kebaberize()}.json";
            if (download.Content.CanSeek) download.Content.Seek(0, SeekOrigin.Begin);
            await Files.DownloadFileFromStreamAsync(fileName, download.Content);
        }

        public async Task ExportFromJsonDataAsync(string fileName,string JsonData, CancellationToken cancellationToken = default)
        {
            // 将字符串转换为字节流
            byte[] byteArray = Encoding.UTF8.GetBytes(JsonData);
            using MemoryStream stream = new MemoryStream(byteArray);
            await Files.DownloadFileFromStreamAsync(fileName, stream);
        }

        public async Task ImportFromJsonAsync(IReadOnlyList<IBrowserFile> files,ImportOptions? options = null)
        {
            
            var importResults = (await WorkflowDefinitionImporter.ImportFilesAsync(files, options)).ToList();
            var failedImports = importResults.Where(x => !x.IsSuccess).ToList();
            var successfulImports = importResults.Where(x => x.IsSuccess).ToList();


            //if (successfulImports.Count == 1)
            //    UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully imported 1 workflow definition."], Severity.Success, ConfigureSnackbar);
            //else if (importResults.Count > 1)
            //    UserMessageService.ShowSnackbarTextMessage(Localizer["Successfully imported {0} workflow definitions.", importResults.Count], Severity.Success, ConfigureSnackbar);

            //if (failedImports.Count == 1)
            //    UserMessageService.ShowSnackbarTextMessage(Localizer["Failed to import 1 workflow definition: {0}", failedImports[0].Failure!.ErrorMessage], Severity.Error, ConfigureSnackbar);
            //else if (failedImports.Count > 1)
            //    UserMessageService.ShowSnackbarTextMessage(Localizer["Failed to import {0} workflow definitions. Errors: {1}", failedImports.Count, string.Join(", ", failedImports.Select(x => x.Failure!.ErrorMessage))], Severity.Error, ConfigureSnackbar);

            return;
            void ConfigureSnackbar(SnackbarOptions snackbarOptions)
            {
                snackbarOptions.SnackbarVariant = Variant.Filled;
                snackbarOptions.CloseAfterNavigation = failedImports.Count > 0;
                snackbarOptions.VisibleStateDuration = failedImports.Count > 0 ? 10000 : 3000;
            }
        }
    }
}
