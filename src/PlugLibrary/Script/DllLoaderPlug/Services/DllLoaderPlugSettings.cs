using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using DllLoaderPlug;
using DllLoaderPlug.Pages;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace DllLoaderPlug.Services
{
    public class DllLoaderPlugSettings : IPlugCommonSettingContent
    {
        //实现插头主配置界面内容
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<DllLoaderPlugPage>(sequence++);
                    //builder.AddAttribute(sequence++, nameof(CSharpPlugCommonSettingPage.), context);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }


        //实现插头基础配置信息
        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings(null);
            settings.PlugType = PlugKeySetting.CommonSettingPageKey;
            settings.PlugDisplayName = "DLL集成组件";
            settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.DllPath.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = true
            });
            

            settings.SetSetting(PlugSettingKey.InitVariables.ToString(),
                JsonSerializer.Serialize(InitVariables));

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.脚本执行.ToString());

            settings.SetSetting(PlugSettingKey.Outcomes.ToString(), string.Join("|", Enum.GetNames(typeof(InitOutcomes))));

            return Task.FromResult<PlugSettings?>(settings);
        }
    }
}
