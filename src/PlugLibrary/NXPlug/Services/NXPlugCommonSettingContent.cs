
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
    public class NXPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private NXPlugCommonSettingPage? _designerWrapper;
        
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.NXPlug.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {
                    builder.OpenComponent<NXPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(NXPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (NXPlugCommonSettingPage)@ref);
                    builder.CloseComponent();
                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugTypeKey = PlugKeySetting.NXPlug.CommonSettingPageKey;
            settings.PlugDisplayName = "NX组件";
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = NXPlugVariables.NXFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXPlugVariables.StlFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = NXPlugVariables.ModelParameters.ToString(),
                Type = VariableTypeEnum.ModelParameters.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }

        /// <summary>
        /// NX 插头的默认子插头（预置动作）:
        /// 获取参数、设置参数、模型转 STL —— 对应三个 NX 子工具，开箱即用。
        /// </summary>
        public Task<List<Plug>?> GetDefaultChildPlugs()
        {
            var children = new List<Plug>
            {
                new()
                {
                    Name = "获取NX模型参数",
                    PlugTypeKey = PlugKeySetting.NXGetParameters.CommonExecuteKey,  // 走 NXGetParametersPlugCommonExecuteService → StationPlugExecuteService
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "获取NX模型参数(1.0)",
                },
                new()
                {
                    Name = "设置NX模型参数",
                    PlugTypeKey = PlugKeySetting.NXSetParameters.CommonExecuteKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "设置NX模型参数(1.0)",
                },
                new()
                {
                    Name = "NX模型转STL",
                    PlugTypeKey = PlugKeySetting.NXToStl.CommonExecuteKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    ToolDisplayName = "NX模型转STL(1.0)",
                },
            };
            return Task.FromResult<List<Plug>?>(children);
        }
    }
}
