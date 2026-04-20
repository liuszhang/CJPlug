
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using NXPlug_AIModel;
using NXPlug_AIModel.Pages;

namespace NXPlug_AIModel.Services
{
    public class NXAIModelPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private NXAIModelPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<NXAIModelPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(NXAIModelPlugCommonSettingPage.Plug), Plug);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (NXAIModelPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugType = "NXAIModel";
            PlugSettings.PlugDisplayName = "NXAIModel";
            PlugSettings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
            //PlugSettings.SetSetting(PlugSettingKey.Outcomes.ToString(), "成功|失败");
            PlugSettings.SetSetting("WaitType", "waitforever");
            PlugSettings.SetSetting("WaitTime", "0");
            PlugSettings.SetSetting("WatchProcessType", "default");
            PlugSettings.SetSetting("WatchProcessName", "");
            PlugSettings.SetSetting("AutoCloseProcess", "false");
            PlugSettings.SetSetting("CloseProcessWhenError", "false");
            PlugSettings.SetSetting("ShowConsole", "true");
            PlugSettings.SetSetting("RedirectOutput", "true");
            PlugSettings.SetSetting("RedirectWorkPath", "true");

            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());


            return Task.FromResult<PlugSettings?>(PlugSettings);
        }
    }
}
