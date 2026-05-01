using CJ.Plug.Models.Plug;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

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

        public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
        {
            var AllPlugs = MainApiClient.GetPlugs().Result;
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
                Console.WriteLine($"{x.Name}(category:{x.Category})(type:{x.Type})(group:{x.GroupName})(show:{x.ShowInPlugLibrary})");
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
                    TypeName =  x.Type ?? x.Name,
                    Name = $"{x.Name}",
                    Namespace = "CJ",
                    DisplayName = $"{x.Name}",
                    Category = x.GroupName??"",
                    Description = "插头描述："+x.Description,
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

            return new(activities);
        }

    }
}
