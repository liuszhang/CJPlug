
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using PythonPlug.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PythonPlug.Services
{
    public class PythonPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private PythonPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<PythonPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.Flowchart), flowchart);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (PythonPlugCommonSettingPage)@ref);

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
            settings.PlugDisplayName = "Python组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
