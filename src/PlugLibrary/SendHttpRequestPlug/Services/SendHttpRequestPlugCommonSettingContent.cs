using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace SendHttpRequestPlug.Services
{
    public class SendHttpRequestPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugType = PlugKeySetting.PlugTypeName;
            settings.PlugTypeKey = PlugKeySetting.PlugTypeKey;
            settings.PlugDisplayName = "基础HTTP请求";

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable() { Name = InitVariableNames.Url.ToString(), Type = VariableTypeEnum.String.ToString() });
            InitVariables.Add(new BaseVariable() { Name = InitVariableNames.Method.ToString(), Type = VariableTypeEnum.String.ToString() });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.RequestHeaders.ToString(),
                Type = VariableTypeEnum.RequestHeader.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));


            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.接口集成.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
