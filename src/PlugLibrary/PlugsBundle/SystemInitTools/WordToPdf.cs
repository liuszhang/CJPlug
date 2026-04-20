using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Models;

namespace PlugsBundle.SystemInitTools
{
    public class WordToPdf : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugType = "WordToPdf";
            settings.PlugDisplayName = "Word转Pdf";
            settings.PlugTypeKey = "";
            settings.SetSetting(PlugSettingKey.Category.ToString(),PlugCategorys.桌面类.ToString());
            settings.SetSetting(PlugSettingKey.ToolPath.ToString(), "D:\\99_Pro\\CJ.Plug-Aspire\\PlugToolIntegrations\\WordToPdf\\bin\\Debug\\net4.8\\WordToPdf.exe");
            settings.SetSetting(PlugSettingKey.CommandLineShema.ToString(), "[WordFilePath] [PdfFilePath]");
            settings.SetSetting(PlugSettingKey.InitVariables.ToString(), "WordFilePath PdfFilePath");

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
