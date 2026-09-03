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
    /// Catia 设置模型参数插头的通用设置内容
    /// </summary>
    public class CatiaSetParametersPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CatiaSetParameters.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaSetParametersPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaSetParametersPlugCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugDisplayName = "设置Catia模型参数";
            PlugSettings.PlugTypeKey = PlugKeySetting.CatiaSetParameters.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaSetParametersVariables.ModelFilePath.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaSetParametersVariables.NewParameterString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaSetParametersVariables.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });

            PlugSettings.InitVariables = InitVariables;
            PlugSettings.SetSetting(PlugSettingKey.Category.ToString(), PlugCategorys.桌面类.ToString());
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            PlugSettings.SetSetting(PlugSettingKey.ToolDisplayName.ToString(), "设置Catia模型参数(1.0)");

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }
    }
}
