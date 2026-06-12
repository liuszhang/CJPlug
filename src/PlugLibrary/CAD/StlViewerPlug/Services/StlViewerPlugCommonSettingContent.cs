using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.PlugBaseCore.Models;
using Microsoft.AspNetCore.Components;
using StlViewerPlug.Pages;
using System.Text.Json;

namespace StlViewerPlug.Services;

public class StlViewerPlugCommonSettingContent : IPlugCommonSettingContent
{
    public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
    {
        if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
        {
            var seq = 0;
            return Task.FromResult<RenderFragment?>(builder =>
            {
                builder.OpenComponent<StlViewerPlugCommonSettingPage>(seq++);
                builder.SetKey(context.PlugTypeKey);
                builder.AddAttribute(seq++, nameof(StlViewerPlugCommonSettingPage.PlugDefinitionId), context.PlugDefinitionId);
                builder.CloseComponent();
            });
        }
        return Task.FromResult<RenderFragment?>(null);
    }

    public Task<PlugSettings?> GetPlugBaseSetting()
    {
        var settings = new PlugSettings(null);
        settings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
        settings.PlugDisplayName = "STL 查看器";
        settings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());

        var initVars = new List<BaseVariable>
        {
            new() { Name = InitVariableNames.StlFile.ToString(), Type = VariableTypeEnum.File.ToString() }
        };
        settings.SetSetting(PlugSettingKey.InitVariables.ToString(), JsonSerializer.Serialize(initVars));

        return Task.FromResult<PlugSettings?>(settings);
    }
}
