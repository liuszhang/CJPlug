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
    /// Catia 获取模型参数插头的动作设置内容
    /// </summary>
    public class CatiaGetParametersPlugActionSettingContent : IPlugActionSettingContent
    {
        private CatiaGetParametersPlugActionSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.CatiaGetParameters.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaGetParametersPlugActionSettingPage>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaGetParametersPlugActionSettingPage.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CatiaGetParametersPlugActionSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }
    }
}
