
using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using WordPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using System.Text.Json;
using CJ.Plug.PlugBaseCore.Models;

namespace WordPlug.Services
{
    public class WordPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private WordPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<WordPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(WordPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    //builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.Flowchart), flowchart);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (WordPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Word组件";
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WordFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WordTextMapping.ToString(),
                Type = VariableTypeEnum.WordTextMapping.ToString(),
                IsBrowsable = true,
                IsArray = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ResultFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
