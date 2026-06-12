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
    public class NXToStlCommonSettingContent : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            // NX模型转STL 使用通用设置页面
            if (context.PlugTypeKey == PlugKeySetting.NXToStl.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<NXToStlCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(NXToStlCommonSettingPage.SettingContext), context);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugDisplayName = "NX模型转STL";
            PlugSettings.PlugTypeKey = PlugKeySetting.NXToStl.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.PrtFilePath.ToString(),
                Type = VariableTypeEnum.File.ToString(),
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.StlOutputPath.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.ChordalTol.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                Value = "0.08",
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.AdjacencyTol.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                Value = "0.08",
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.AutoNormalGen.ToString(),
                Type = VariableTypeEnum.Bool.ToString(),
                Value = "true",
                IsBrowsable = true,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXToStlVariables.ResultString.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            PlugSettings.InitVariables = InitVariables;

            PlugSettings.SetSetting(PlugSettingKey.Category.ToString(), PlugCategorys.桌面类.ToString());
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            PlugSettings.SetSetting(PlugSettingKey.ToolDisplayName.ToString(), "NX模型转STL(1.0)");

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }
    }
}
