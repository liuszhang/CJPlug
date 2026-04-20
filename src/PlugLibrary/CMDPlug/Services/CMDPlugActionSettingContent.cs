using CJ.Plug.Models.PlugAction;
using Microsoft.AspNetCore.Components;
using CMDPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;

namespace CMDPlug.Services
{
    public class CMDPlugActionSettingContent : IPlugActionSettingContent
    {

        private CMDPlugActionSettingPage_ExecuteCMD? _designerWrapper;
        public Task<RenderFragment?> GetPlugActionSettingContent(Plug ActionItem)
        {
            if (ActionItem.PlugTypeKey == PlugKeySetting.ActionExecuteKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<CMDPlugActionSettingPage_ExecuteCMD>(sequence++);
                    builder.SetKey(ActionItem.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CMDPlugActionSettingPage_ExecuteCMD.Plug), ActionItem);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CMDPlugActionSettingPage_ExecuteCMD)@ref);

                    builder.CloseComponent();
                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }        
    }
}
