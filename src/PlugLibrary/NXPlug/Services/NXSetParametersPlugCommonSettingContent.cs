using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using NXPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;
using NXPlug;

namespace NXPlug.Services
{
    public class NXSetParametersPlugCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.NXSetParameters.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<NXSetParametersPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(NXSetParametersPlugCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugType = PlugKeySetting.NXSetParameters.CommonSettingPageKey;
            PlugSettings.PlugDisplayName = "设置NX模型参数";
            PlugSettings.PlugTypeKey = PlugKeySetting.NXSetParameters.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = NXSetParametersVariables.ModelFilePath.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                DefaultValue = "",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXSetParametersVariables.NewParameterString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                DefaultValue = "",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXSetParametersVariables.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            
            PlugSettings.InitVariables = InitVariables;
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }

        /// <summary>
        /// NX设置参数插头的的默认子插头：常用参数设置示例
        /// </summary>
        public Task<List<Plug>?> GetDefaultChildPlugs()
        {
            var children = new List<Plug>
            {
                new()
                {
                    Name = "设置NX模型参数示例",
                    Type = PlugKeySetting.NXSetParameters.CommonSettingPageKey,
                    PlugTypeKey = PlugKeySetting.NXSetParameters.CommonSettingPageKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    PlugVariables = new List<PlugVariable>
                    {
                        new() { Name = NXSetParametersVariables.ModelFilePath.ToString(), Value = "", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true },
                        new() { Name = NXSetParametersVariables.NewParameterString.ToString(), Value = "", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true }
                    }
                }
            };
            return Task.FromResult<List<Plug>?>(children);
        }
    }
}
