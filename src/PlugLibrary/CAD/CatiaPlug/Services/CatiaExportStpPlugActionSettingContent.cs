using CJ.Plug.Models.PlugAction;
using Microsoft.AspNetCore.Components;
using CatiaPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;
using CatiaPlug;

namespace CatiaPlug.Services
{
    /// <summary>
    /// Catia 导出STP插头的动作设置内容
    /// </summary>
    public class CatiaExportStpPlugActionSettingContent : IPlugActionSettingContent
    {
        private CatiaExportStpPlugActionSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.CatiaExportStp.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaExportStpPlugActionSettingPage>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaExportStpPlugActionSettingPage.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CatiaExportStpPlugActionSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }
    }
}
