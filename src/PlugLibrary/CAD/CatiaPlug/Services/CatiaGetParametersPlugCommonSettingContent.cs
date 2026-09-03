using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using CatiaPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;
using CatiaPlug;

namespace CatiaPlug.Services
{
    /// <summary>
    /// Catia 获取模型参数插头的通用设置内容
    /// </summary>
    public class CatiaGetParametersPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CatiaGetParameters.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaGetParametersPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaGetParametersPlugCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugDisplayName = "获取Catia模型参数";
            PlugSettings.PlugTypeKey = PlugKeySetting.CatiaGetParameters.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaGetParametersVariables.ModelFilePath.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaGetParametersVariables.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            PlugSettings.InitVariables = InitVariables;
            PlugSettings.SetSetting(PlugSettingKey.Category.ToString(), PlugCategorys.桌面类.ToString());
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            PlugSettings.SetSetting(PlugSettingKey.ToolDisplayName.ToString(), "获取Catia模型参数(1.0)");

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }
    }
}
