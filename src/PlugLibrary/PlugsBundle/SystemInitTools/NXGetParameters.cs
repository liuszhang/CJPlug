using Microsoft.AspNetCore.Components;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;

namespace PlugsBundle.SystemInitTools.NXGetParameters
{
    public class NXGetParameters : IPlugCommonSettingContent
    {
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var settings = new PlugSettings();
            settings.PlugDisplayName = "获取NX模型参数";
            settings.PlugTypeKey = "";  //设为空，走默认实现的插头执行逻辑（CMD）

            settings.SetSetting(PlugSettingKey.ToolName.ToString(), "获取NX模型参数");
            settings.SetSetting(PlugSettingKey.ToolVersion.ToString(), "1.0");

            settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            settings.SetSetting(PlugSettingKey.Category.ToString(), PlugCategorys.桌面类.ToString());

            return Task.FromResult<PlugSettings?>(settings);
        }
    }

    public enum InitVariableNames
    {
        ModelFilePath,
        ModelFileId
    }

    public class PlugKeySetting
    {
        //这里界面和执行方法的KEY分开配置是为后期如需要分开设定KEY预留

        public static string PlugTypeName = "NXGetParameters";
        public static string PlugTypeKey = "NXGetParameters";
        public static string CommonSettingPageKey = "";
        public static string CommonExecuteKey = "";
    }
}
