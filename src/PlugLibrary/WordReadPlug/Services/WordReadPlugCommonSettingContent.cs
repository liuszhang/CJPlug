using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using WordReadPlug.Pages;

namespace WordReadPlug.Services
{
    public class WordReadPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private WordReadPlugCommonSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<WordReadPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(WordReadPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (WordReadPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Word读取组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var initVariables = new List<BaseVariable>();
            initVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WordFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });
            initVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ReadResult.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(initVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
