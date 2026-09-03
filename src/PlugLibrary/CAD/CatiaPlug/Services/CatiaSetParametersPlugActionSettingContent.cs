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
    /// Catia 设置模型参数插头的动作设置内容
    /// </summary>
    public class CatiaSetParametersPlugActionSettingContent : IPlugActionSettingContent
    {
        private CatiaSetParametersPlugActionSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.CatiaSetParameters.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaSetParametersPlugActionSettingPage>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaSetParametersPlugActionSettingPage.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CatiaSetParametersPlugActionSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }
    }
}
