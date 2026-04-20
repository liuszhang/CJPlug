
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

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<ExcelPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(ExcelPlugCommonSettingPage.Plug), Plug);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (ExcelPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugDisplayName = "Excel组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ExcelFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));
            settings.SetSetting(PlugSettingKey.IsContainerPlug.ToString(),
                "false");
            settings.SetSetting(PlugSettingKey.Group.ToString(),
                PlugGroupEnum.工具集成.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
