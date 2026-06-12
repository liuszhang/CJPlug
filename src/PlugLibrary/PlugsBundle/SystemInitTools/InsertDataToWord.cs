using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;

namespace PlugsBundle.SystemInitTools
{
    public class InsertDataToWord : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "插入数据至Word书签";
            settings.PlugTypeKey = "";
            settings.SetSetting(PlugSettingKey.ToolPath.ToString(), "D:\\99_Pro\\CJ.Plug-Aspire\\PlugToolIntegrations\\WordInsertData\\bin\\Debug\\net8.0\\WordInsertData.exe");
            settings.SetSetting(PlugSettingKey.CommandLineShema.ToString(), "[WordFilePath] [BmName] [TextToInsert]");
            settings.SetSetting(PlugSettingKey.InitVariables.ToString(), "WordFilePath BmName TextToInsert");

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
