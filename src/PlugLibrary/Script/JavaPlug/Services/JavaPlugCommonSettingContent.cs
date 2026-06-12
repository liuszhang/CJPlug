using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using JavaPlug.Pages;
using Microsoft.AspNetCore.Components;

namespace JavaPlug.Services
{
    public class JavaPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private JavaPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<JavaPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(JavaPlugCommonSettingPage.SettingContext), context);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (JavaPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Java脚本组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.Script.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            settings.InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ScriptType.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
                Value = "Java"
            });

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
