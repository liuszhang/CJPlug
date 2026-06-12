
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;
using TextWriterPlug.Pages;

namespace TextWriterPlug.Services
{
    public class TextWriterPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<TextWriterPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(TextWriterPlugCommonSettingPage.SettingContext), context);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugDisplayName = "文本解析-写";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.TextFile.ToString(),
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
                Name = InitVariableNames.ResultFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            //以名称：类型的方式设置初始化参数
            //settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
            //    $"{InitVariableNames.TextFile.ToString()}:{PlugVariableTypeEnum.文件.ToString()}");

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
