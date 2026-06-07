using CJ.Plug.Models.PlugAction;
using Microsoft.AspNetCore.Components;
using NXPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;
using NXPlug;

namespace NXPlug.Services
{
    public class NXSetParametersPlugActionSettingContent : IPlugActionSettingContent
    {
        private NXSetParametersPlugActionSettingPage? _designerWrapper;
        
        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.NXSetParameters.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<NXSetParametersPlugActionSettingPage>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(NXSetParametersPlugActionSettingPage.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (NXSetParametersPlugActionSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }        
    }
}
