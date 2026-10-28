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
    /// Catia 导出STL插头的动作设置内容
    /// </summary>
    public class CatiaExportStlPlugActionSettingContent : IPlugActionSettingContent
    {
        private CatiaExportStlPlugActionSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.CatiaExportStl.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaExportStlPlugActionSettingPage>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaExportStlPlugActionSettingPage.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CatiaExportStlPlugActionSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }
    }
}
