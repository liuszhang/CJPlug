
using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using NXPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;

namespace NXPlug.Services
{
    public class NXPlugCommonSettingContent : IPlugCommonSettingContent
    {
        private NXPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<NXPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    //builder.AddAttribute(sequence++, nameof(PythonPlugCommonSettingPage.Flowchart), flowchart);
                    builder.AddAttribute(sequence++, nameof(NXPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                    //builder.AddAttribute(sequence++, nameof(NXPlugCommonSettingPage.PlugData), context.Plug);
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
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
            settings.PlugDisplayName = "NX组件";
            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.NXFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.StlFile.ToString(),
                Type = VariableTypeEnum.File.ToString(),
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ModelParameters.ToString(),
                Type = VariableTypeEnum.ModelParameters.ToString(),
                IsBrowsable = true,
                IsArray = true
            });

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
