using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using LLMPlug.Pages;
using CJ.Plug.PlugBaseCore.Models;

namespace LLMPlug.Services
{
    public class LLMPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private LLMPlugCommonSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<LLMPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(LLMPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (LLMPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "大模型调用组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var initVariables = new List<BaseVariable>();
            initVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Question.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            initVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Thinking.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            initVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Answer.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(initVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
