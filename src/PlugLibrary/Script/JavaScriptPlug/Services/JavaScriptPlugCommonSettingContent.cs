
using CJ.Plug.Models.Plug;
using JavaScriptPlug.Pages;
using Microsoft.AspNetCore.Components;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;

namespace JavaScriptPlug.Services
{
    public class JavaScriptPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private JavaScriptPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<JavaScriptPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.Flowchart), flowchart);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (JavaScriptPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugDisplayName = "JavaScript脚本";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
