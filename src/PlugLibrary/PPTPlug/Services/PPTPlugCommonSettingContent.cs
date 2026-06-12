using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using PPTPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using System.Text.Json;
using CJ.Plug.PlugBaseCore.Models;

namespace PPTPlug.Services
{
    public class PPTPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private PPTPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            // 鏍规嵁涓嶅悓鐨勬彃浠剁被鍨嬭繑鍥炰笉鍚岀殑娓叉煋鐗囨
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<PPTPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(PPTPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (PPTPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            // 濡傛灉娌℃湁鍖归厤鐨勬彃浠剁被鍨嬶紝鍒欒繑鍥瀗ull鎴栭粯璁ょ殑RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "PPT组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.PPTFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.PPTTextMapping.ToString(),
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

