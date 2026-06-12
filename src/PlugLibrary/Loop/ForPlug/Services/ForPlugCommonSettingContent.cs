
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using ForPlug.Pages;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace ForPlug.Services
{
    public class ForPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<ForPlugCommonSettingPage>(sequence++);
                    builder.AddAttribute(sequence++, nameof(ForPlugCommonSettingPage.Plug), context.Plug);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "For循环";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.From.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.To.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Step.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));
            //settings.SetSetting(PlugSettingKey.IsContainerPlug.ToString(),
            //    "true");
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.流程控制.ToString());

            settings.SetSetting(PlugSettingKey.Outcomes.ToString(), "结束|循环");

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}

