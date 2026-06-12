using AIAgentPlug.Pages;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace AiAgentPlug.Services
{
    public class AiAgentPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<AiAgentPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(AiAgentPlugCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugDisplayName = "AiAgent";
            PlugSettings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = "AIModelName",
                Type = "String",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = "AISystemPrompt",
                Type = "String",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = "AIUserPrompt",
                Type = "String",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = "AIOutputVariable",
                Type = "String",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = "AIInputParameters",
                Type = "String",
                IsBrowsable = false,
            });

            PlugSettings.SetSetting(PlugSettingKey.InitVariables.ToString(), JsonSerializer.Serialize(InitVariables));
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }
    }
}
