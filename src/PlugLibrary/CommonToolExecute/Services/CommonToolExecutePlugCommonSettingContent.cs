
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CommonToolExecute.Pages;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace CommonToolExecute.Services
{
    public class CommonToolExecutePlugCommonSettingContent : IPlugCommonSettingContent
    {
        private CommonToolExecutePlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<CommonToolExecutePlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.Flowchart), flowchart);
                    //builder.AddAttribute(sequence++, nameof(CommonToolExecutePlugCommonSettingPage.Plug), Plug);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CommonToolExecutePlugCommonSettingPage)@ref);

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
            settings.PlugDisplayName = "通用工具执行";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());


            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Tool.ToString(),
                Type = VariableTypeEnum.ToolVariable.ToString(),
                IsBrowsable = true,
                IsArray = false
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ToolCommandVariable.ToString(),
                Type = VariableTypeEnum.ToolCommandVariable.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));


            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
