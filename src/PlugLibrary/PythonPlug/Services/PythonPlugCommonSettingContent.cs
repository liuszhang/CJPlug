using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using Microsoft.AspNetCore.Components;
using PythonPlug.Pages;
using System.Text.Json;

namespace PythonPlug.Services
{
    public class PythonPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private PythonPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 鏍规嵁涓嶅悓鐨勬彃浠剁被鍨嬭繑鍥炰笉鍚岀殑娓叉煋鐗囨
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<PythonPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.SettingContext), context);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (PythonPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 濡傛灉娌℃湁鍖归厤鐨勬彃浠剁被鍨嬶紝鍒欒繑鍥瀗ull鎴栭粯璁ょ殑RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Python脚本组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Script.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.InputData.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ScriptType.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
                Value = "Python"
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}

