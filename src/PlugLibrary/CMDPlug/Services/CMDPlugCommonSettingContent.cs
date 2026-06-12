using CJ.Plug.Models.Plug;
using Microsoft.AspNetCore.Components;
using CMDPlug.Pages;
using CJ.Plug.ApiClient.Contracts;
using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.Models.Shared;
using System.Text.Json;
using CJ.Plug.Models.VariableType;
using CJ.Plug.PlugBaseCore.Models;

namespace CMDPlug.Services
{
    public class CMDPlugCommonSettingContent : IPlugCommonSettingContent
    {
        //private CMDPlugCommonSettingPage? _designerWrapper;
        public Task<RenderFragment?> GetPlugCommonSettingContent(GetSettingContext context)
        {

            // 根据不同的插件类型返回不同的渲染片段
            if (context.PlugTypeKey == PlugKeySetting.CommonSettingPageKey)
            {
                var sequence = 0;
                return Task.FromResult<RenderFragment?>(builder =>
                {

                    builder.OpenComponent<CMDPlugCommonSettingPage>(sequence++);
                    builder.SetKey(context.PlugTypeKey);
                    builder.AddAttribute(sequence++, nameof(CMDPlugCommonSettingPage.SettingContext), context);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.IsReadOnly), context.IsReadOnly);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityStats), context.ActivityStats);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivitySelected), context.ActivitySelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityEmbeddedPortSelected), context.ActivityEmbeddedPortSelectedCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.ActivityDoubleClick), context.ActivityDoubleClickCallback);
                    //builder.AddAttribute(sequence++, nameof(FlowchartDesignerWrapper.GraphUpdated), context.GraphUpdatedCallback);
                    //builder.AddComponentReferenceCapture(sequence++, @ref => _designerWrapper = (CMDPlugCommonSettingPage)@ref);

                    builder.CloseComponent();

                });
            }

            // 如果没有匹配的插件类型，则返回null或默认的RenderFragment
            return Task.FromResult<RenderFragment?>(null);
        }

        public Task<PlugSettings?> GetPlugBaseSetting()
        {
            var PlugSettings = new PlugSettings(null);
            PlugSettings.PlugDisplayName = "CMD命令执行";
            PlugSettings.PlugTypeKey = PlugKeySetting.CommonSettingPageKey;
            //PlugSettings.SetSetting(PlugSettingKey.Outcomes.ToString(), "成功|失败");

            var InitVariables = new List<BaseVariable>();
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.CMDCommand.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                Value = "notepad",
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ExecutionTimeout.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WaitType.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WatchProcessType.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.WatchProcessName.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.AutoCloseProcess.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.CloseProcessWhenError.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.ShowConsole.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.RedirectOutput.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.RedirectWorkPath.ToString(),
                Type = VariableTypeEnum.String.ToString(),
                IsBrowsable = false,
            });
            InitVariables.Add(new BaseVariable()
            {
                Name = InitVariableNames.SupportRemoteView.ToString(),
                Type = VariableTypeEnum.Bool.ToString(),
                Value = "true",
                IsBrowsable = false,
            });
            PlugSettings.InitVariables = InitVariables;

            //PlugSettings.SetSetting(PlugSettingKey.InitVariables.ToString(),JsonSerializer.Serialize(InitVariables));
            PlugSettings.SetSetting(PlugSettingKey.Group.ToString(), PlugGroupEnum.工具集成.ToString());
            PlugSettings.SetSetting(PlugSettingKey.SupportRemoteView.ToString(), "true");

            return Task.FromResult<PlugSettings?>(PlugSettings);
        }

        /// <summary>
        /// CMD 插头的默认子插头：4 个常用命令，开箱即用。
        /// </summary>
        public Task<List<Plug>?> GetDefaultChildPlugs()
        {
            var children = new List<Plug>
            {
                new()
                {
                    Name = "启动记事本",
                    PlugTypeKey = PlugKeySetting.CommonSettingPageKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    PlugVariables = new List<PlugVariable>
                    {
                        new() { Name = InitVariableNames.CMDCommand.ToString(), Value = "notepad", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true }
                    }
                },
                new()
                {
                    Name = "查看IP配置",
                    PlugTypeKey = PlugKeySetting.CommonSettingPageKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    PlugVariables = new List<PlugVariable>
                    {
                        new() { Name = InitVariableNames.CMDCommand.ToString(), Value = "ipconfig /all", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true }
                    }
                },
                new()
                {
                    Name = "列出目录文件",
                    PlugTypeKey = PlugKeySetting.CommonSettingPageKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    PlugVariables = new List<PlugVariable>
                    {
                        new() { Name = InitVariableNames.CMDCommand.ToString(), Value = "dir", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true }
                    }
                },
                new()
                {
                    Name = "系统信息",
                    PlugTypeKey = PlugKeySetting.CommonSettingPageKey,
                    Category = PlugCategorys.桌面类动作.ToString(),
                    CreateType = PlugCreateTypeEnum.SystemInitActionPlug.ToString(),
                    Creater = PlugCreateTypeEnum.SystemInitPlug.ToString(),
                    ShowInPlugLibrary = false,
                    PlugVariables = new List<PlugVariable>
                    {
                        new() { Name = InitVariableNames.CMDCommand.ToString(), Value = "systeminfo", Type = VariableTypeEnum.String.ToString(), IsInitVariable = true }
                    }
                }
            };
            return Task.FromResult<List<Plug>?>(children);
        }
    }
}
