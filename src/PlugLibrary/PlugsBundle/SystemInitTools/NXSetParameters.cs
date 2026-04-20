using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace PlugsBundle.SystemInitTools
{
    public class NXSetParameters : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings();
            settings.PlugType = "NXSetParameters";
            settings.PlugDisplayName = "设置NX模型参数";
            settings.PlugTypeKey = "";
            settings.SetSetting(PlugSettingKey.Category.ToString(),PlugCategorys.桌面类.ToString());
            //settings.SetSetting(PlugSettingKey.ToolPath.ToString(), "D:\\99_Pro\\CJ.Plug-Aspire\\PlugToolIntegrations\\NXUpdateParameters\\bin\\Debug\\net4.8\\NXUpdateParameters.exe");
            settings.SetSetting(PlugSettingKey.ToolDisplayName.ToString(), "设置NX模型参数(1.0)");
            //settings.SetSetting(PlugSettingKey.CommandLineShema.ToString(), "[ModelFilePath] [NewParameterString]");
            //settings.SetSetting(PlugSettingKey.InitVariables.ToString(), "ModelFilePath NewParameterString");

            //var InitVariables = new List<BaseVariable>();
            //InitVariables.Add(new BaseVariable()
            //{
            //    Name = "ModelFilePath",
            //    Type = VariableTypeEnum.String.ToString(),
            //});
            //InitVariables.Add(new BaseVariable()
            //{
            //    Name = "NewParameterString",
            //    Type = VariableTypeEnum.String.ToString(),
            //});


            //settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
            //    JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());


            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
