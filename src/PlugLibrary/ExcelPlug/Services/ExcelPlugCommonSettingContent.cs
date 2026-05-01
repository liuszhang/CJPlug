using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using ExcelPlug.Pages;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace ExcelPlug.Services
{
    public class ExcelPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private ExcelPlugCommonSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<ExcelPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(ExcelPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (ExcelPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Excel组件";
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ExcelFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.TextMapping.ToString(),
                Type = VariableTypeEnum.TextMapping.ToString(),
                IsBrowsable = true,
                IsArray = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ConditionalExpressions.ToString(),
                Type = VariableTypeEnum.String.ToString(),
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
