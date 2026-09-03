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
    /// Catia 主插头通用设置内容
    /// </summary>
    public class CatiaPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private CatiaPlugCommonSettingPage? _designerWrapper;

        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            if (context.PlugTypeKey == PlugKeySetting.CatiaPlug.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<CatiaPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CatiaPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CatiaPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugTypeKey = PlugKeySetting.CatiaPlug.CommonSettingPageKey;
            settings.PlugDisplayName = "Catia组件";
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaPlugVariables.CatiaFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaPlugVariables.StlFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = CatiaPlugVariables.ModelParameters.ToString(),
                Type = VariableTypeEnum.ModelParameters.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }

        /// <summary>
        /// Catia 主插头的默认子插头（预置动作）：
        /// 获取参数 / 设置参数 / 导出STP / 导出STL —— 对应四个 Catia 子工具，开箱即用。
        /// </summary>
        public Task<List<Plug>?> GetDefaultChildPlugs()
        {
            var children = new List<Plug>
            {
                new()
                {
                    Name = "获取Catia模型参数",
                    PlugTypeKey = PlugKeySetting.CatiaGetParameters.CommonExecuteKey, // 走 CatiaGetParametersCommonExecuteService → StationPlugExecuteService
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "获取Catia模型参数(1.0)",
                },
                new()
                {
                    Name = "设置Catia模型参数",
                    PlugTypeKey = PlugKeySetting.CatiaSetParameters.CommonExecuteKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "设置Catia模型参数(1.0)",
                },
                new()
                {
                    Name = "Catia模型转STP",
                    PlugTypeKey = PlugKeySetting.CatiaExportStp.CommonExecuteKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "Catia模型转STP(1.0)",
                },
                new()
                {
                    Name = "Catia模型转STL",
                    PlugTypeKey = PlugKeySetting.CatiaExportStl.CommonExecuteKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "Catia模型转STL(1.0)",
                },
            };
            return Task.FromResult<List<Plug>?>(children);
        }
    }
}
