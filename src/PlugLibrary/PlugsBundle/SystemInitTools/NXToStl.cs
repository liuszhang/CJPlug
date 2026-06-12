using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace PlugsBundle.SystemInitTools
{
    public class NXToStl : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings();
            settings.PlugDisplayName = "NX模型转STL";
            settings.PlugTypeKey = "";
            settings.SetSetting(PlugSettingKey.Category.ToString(),PlugCategorys.桌面类.ToString());
            settings.SetSetting(PlugSettingKey.Group.ToString(),PlugGroupEnum.工具集成.ToString());
            //settings.SetSetting(PlugSettingKey.ToolPath.ToString(), "D:\\99_Pro\\CJ.Plug-Aspire\\PlugToolIntegrations\\NXToStl\\bin\\Debug\\net4.8\\NXToStl.exe");
            settings.SetSetting(PlugSettingKey.ToolDisplayName.ToString(), "NX模型转STL(1.0)");
            //settings.SetSetting(PlugSettingKey.CommandLineShema.ToString(), "[ModelFilePath] [StlFilePath]");
            //var InitVariables = new List<BaseVariable>();
            //InitVariables.Add(new BaseVariable()
            //{
            //    Name = "ModelFilePath",
            //    Type = VariableTypeEnum.File.ToString(),
            //    IsBrowsable = true
            //});
            //InitVariables.Add(new BaseVariable()
            //{
            //    Name = "StlFilePath",
            //    Type = VariableTypeEnum.String.ToString(),
            //    IsBrowsable = true
            //});

            //settings.SetSetting(PlugSettingKey.InitVariables.ToString(),JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
