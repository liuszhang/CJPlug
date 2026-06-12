
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CSharpPlug.Pages;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace CSharpPlug.Services
{
    public class CSharpPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<CSharpPlugCommonSettingPage>(sequence++);
                    builder.AddAttribute(sequence++, nameof(CSharpPlugCommonSettingPage.SettingContext), context);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "C#组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            //var InitVariables = new List<BaseVariable>();
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Script.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.DllReferences.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.EnvironmentVariables.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.UseDotNetFramework.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ScriptType.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
                Value = "CSharp"
            });

            //settings.InitVariables = InitVariables;

            //settings.SetSetting(PlugSettingKey.InitVariables.ToString(),JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            settings.SetSetting(PlugSettingKey.Outcomes.ToString(), string.Join("|", Enum.GetNames(typeof(InitOutcomes))));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
