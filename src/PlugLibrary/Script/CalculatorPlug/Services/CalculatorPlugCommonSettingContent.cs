
using CalculatorPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using Polly;
using System.Text.Json;

namespace CalculatorPlug.Services
{
    public class CalculatorPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<CalculatorPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CalculatorPlugCommonSettingPage.SettingContext), context);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "计算器";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Expression.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Result.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            //settings.SetSetting(PlugSettingKey.Outcomes.ToString(), string.Join("|", Enum.GetNames(typeof(InitOutcomes))));

            return Task.FromResult<PlugSettings?>(settings);
        }

        
    }
}
