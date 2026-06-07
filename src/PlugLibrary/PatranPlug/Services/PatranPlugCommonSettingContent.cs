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
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugDisplayName = "Patran脚本执行";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();

            // 脚本文件（文件类型）
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ScriptFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true,
            });

            // 关键字映射配置（JSON格式，由UI管理，隐藏）
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.KeywordMappingJson.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            // 执行输出结果（隐藏）
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            // 额外命令行参数（隐藏）
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
