using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using PatranPlug.Pages;
using System.Text.Json;

namespace PatranPlug.Services
{
    public class PatranPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<PatranPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(PatranPlugCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "Patran";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();

            // 鑴氭湰鏂囦欢锛堟枃浠剁被鍨嬶級
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ScriptFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true,
            });

            // 鍏抽敭瀛楁槧灏勯厤缃紙JSON鏍煎紡锛岀敱UI绠＄悊锛岄殣钘忥級
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.KeywordMappingJson.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            // 鎵ц杈撳嚭缁撴灉锛堥殣钘忥級
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            // 棰濆鍛戒护琛屽弬鏁帮紙闅愯棌锛?
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.AdditionalArgs.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            settings.SetSetting(PlugSettingKey.CommandLineShema.ToString(), "[ToolPath] -sri [ScriptFile]");

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}

