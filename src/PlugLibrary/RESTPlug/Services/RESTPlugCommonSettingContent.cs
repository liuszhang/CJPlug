
using CJ.Plug.Models.Plug;
using RESTPlug;
using Microsoft.AspNetCore.Components;
using RESTPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using System.Text.Json;
using CJ.Plug.PlugBaseCore.Models;

namespace RESTPlug.Services
{
    public class RESTPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                int sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<RESTPlugSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(RESTPlugSettingPage.PlugDefinitionId), context.PlugDefinitionId);
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
            settings.PlugDisplayName = "REST接口";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            settings.SetSetting(PlugSettingKey.Group.ToString(),
                PlugGroupEnum.接口集成.ToString());


            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable(){Name = InitVariableNames.Url.ToString(),Type = VariableTypeEnum.String.ToString()});
            InitVariables.Add(new BaseVariable(){Name = InitVariableNames.Method.ToString(),Type = VariableTypeEnum.String.ToString()});
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.RequestHeaders.ToString(),
                Type = VariableTypeEnum.RequestHeader.ToString(),
                IsBrowsable = true,
                IsArray = true
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.OutputMappings.ToString(),
                Type = VariableTypeEnum.DefaultOutputMapping.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));



            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
