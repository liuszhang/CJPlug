using CJ.Plug.Models.Plug;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ⚠ 本文件所在命名空间链上有 CJ.Plug 这个**命名空间**，裸写 `Plug` 会被解析成命名空间（CS0118），
//    所以用别名把实体类型钉死（原来靠 var 推断绕开了这个坑，改成显式声明后就绕不开了）。
using PlugEntity = global::CJ.Plug.Models.Plug.Plug;

namespace CJ.Plug.ElsaIntegration.Services
{
    public class CJActivityProvider : IActivityProvider
    {
        //private readonly IActivityFactory activityFactory;
        private MainApiClient MainApiClient;

        public CJActivityProvider(MainApiClient mainApiClient)
        {
            //this.activityFactory = activityFactory;
            this.MainApiClient = mainApiClient;           
        }

        public async ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            List<PlugEntity> AllPlugs;
            try
            {
                // ⚠ 不要再用 .Result 同步阻塞取插头：它会抛 AggregateException，而 Elsa 的 tenant 启动任务是一批
                //    **顺序执行**的（PopulateRegistriesStartupTask 只是其中一个），此处抛异常会把后续任务一起带崩
                //    —— 包括 AdminUserInitializer（身份引导），实测症状为"引擎有管理员却永远种不上、登录
                //    isAuthenticated:false、组件库首屏空白且不自愈"（2026-10-08 复现）。
                //    ApiServer 晚于本服务就绪是常态（27 模块 + DB 迁移），所以这里**降级而不是抛**。
                AllPlugs = await MainApiClient.GetPlugs(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Elsa] ⚠ CJActivityProvider 取插头失败，本次不提供 CJ 插头描述符（不影响引擎内置活动；" +
                                  $"Studio 下次带 Refresh=true 请求时会自然重取）：{ex.Message}");
                return Array.Empty<ActivityDescriptor>();
            }

            if (AllPlugs == null)
            {
                Console.WriteLine("[Elsa] ⚠ CJActivityProvider 取到的插头列表为 null，本次不提供 CJ 插头描述符");
                return Array.Empty<ActivityDescriptor>();
            }
            //AllPlugs.AddRange(MainApiClient.GetWorkflowsAsync().Result);
            //获取用户创建的源插头
            //var RootPlugs = AllPlugs.Where(t => t.IsRootPlug || t.IsSystemInitPlug || t.IsProcessToPlug).ToList();
            var RootPlugs = AllPlugs.Where(
                t => t.CreateType == PlugCreateTypeEnum.RootPlug.ToString() ||
                t.CreateType == PlugCreateTypeEnum.RootAdminPlug.ToString() ||
                t.CreateType == PlugCreateTypeEnum.SystemInitPlug.ToString() ||
                t.CreateType == PlugCreateTypeEnum.ProcessToPlug.ToString() ||
                t.CreateType == PlugCreateTypeEnum.新建流程.ToString() 
            ).ToList();

            var activities = RootPlugs.Select(x =>
            {
                Console.WriteLine($"{x.Name}(category:{x.Category})(type:{x.PlugTypeKey})(group:{x.GroupName})(show:{x.ShowInPlugLibrary})");
                var fullTypeName = $"{x.Name}";
                var outcomes = x.GetPlugSettings().GetSetting(PlugSettingKey.Outcomes.ToString());
                var ports = new List<Port>();
                if (!string.IsNullOrEmpty(outcomes))
                {
                    var outcomeList = outcomes.Split("|");
                    foreach (var o in outcomeList)
                    {
                        ports.Add(new Port() {Name=o, DisplayName = o, Type = PortType.Flow, IsBrowsable = true });
                    }
                }
                
                return new ActivityDescriptor
                {
                    TypeName =  x.PlugTypeKey ?? x.Name,
                    Name = $"{x.Name}",
                    Namespace = "CJ",
                    DisplayName = $"{x.Name}",
                    Category = x.GroupName??"",
                    Description = $"[DefinitionId:{x.DefinitionId}]插头描述："+x.Description,
                    Ports = ports,
                    IsBrowsable = x.ShowInPlugLibrary,
                    IsContainer=x.IsContainerPlug,
                    Constructor = context =>
                    {
                        //var activity = activityFactory.CreateActivity<CommonCorePlugActivity>(context);
                        var activity = context.CreateActivity<CommonCorePlugActivity>();
                        return activity;
                    }
                };
            }).ToList();

            return activities;
        }

    }
}
