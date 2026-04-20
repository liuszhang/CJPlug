using AIAgentPlug.Pages;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;

namespace AiAgentPlug.Services
{
    public class AiAgentPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private AiAgentPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<AiAgentPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(AiAgentPlugCommonSettingPage.), context);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (AiAgentPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugType = "AiAgentPlug";
            PlugSettings.PlugDisplayName = "AiAgent";
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
