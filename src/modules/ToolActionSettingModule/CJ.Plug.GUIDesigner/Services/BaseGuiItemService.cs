using CJ.Plug.GUIDesigner.Contracts;
using CJ.Plug.GUIDesigner.Models.Shared;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner.Services
{
    public class BaseGuiItemService : IGuiItemService
    {
        public bool IsThisGuiItem(string itemName)=> itemName == "default";


        public ItemDisplaySetting? GetItemDisplaySetting()
        {
                return new ItemDisplaySetting
                {
                    ItemName = "default",
                    ItemGroup = "基础组件",
                    ItemType = "默认组件",
                    ItemDisplayName = "默认组件",
                    ItemDescription = "这是一个默认的GUI组件，用于展示基础功能。",
                    ItemIcon = "fa fa-cog", // FontAwesome图标
                    ItemColor = "#007bff" // 蓝色
                };            
        }


        public object? ToAmisObject()
        {
            return GetInputFileObj();
            //return new
            //{
            //    type = "input-number",
            //    label = "数字",
            //    name = "number",
            //    keyboard = true,
            //    originPosition= "left-top",
            //    style= new
            //    {
            //        position = "absolute",
            //        inset = $"10px auto auto 10px",
            //    },
            //};
        }


        public async Task<RenderFragment?> GetItemGuiContent()
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(MudText));
                builder.AddAttribute(1, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
                {
                    textBuilder.AddContent(0, $"默认控件");
                }));
                builder.CloseComponent();
            };
        }

        public async Task<RenderFragment?> GetItemPropertySettingContent()
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(MudText));
                builder.AddAttribute(1, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
                {
                    textBuilder.AddContent(0, $"默认控件属性");
                }));
                builder.CloseComponent();
            };
        }

        private object GetCodeObj()
        {
            var data = new
            {
                type = "page",
                title = "Code组件自定义语言高亮",
                body = new object[]
                {
                new
                {
                    type = "code",
                    customLang = new
                    {
                        name = "myLog",
                        tokens = new object[]
                        {
                            new
                            {
                                name = "custom-error",
                                regex = "\\[error.*",
                                color = "#ff0000",
                                fontStyle = "bold"
                            },
                            new
                            {
                                name = "custom-notice",
                                regex = "\\[notice.*",
                                color = "#FFA500"
                            },
                            new
                            {
                                name = "custom-info",
                                regex = "\\[info.*",
                                color = "#808080"
                            },
                            new
                            {
                                name = "custom-date",
                                regex = "\\[[a-zA-Z 0-9:]+\\]",
                                color = "#008800"
                            }
                        }
                    },
                    value = "[Sun Mar 7 16:02:00 2021] [notice] Apache/1.3.29 (Unix) configured -- resuming normal operations\n[Sun Mar 7 16:02:00 2021] [info] Server built: Feb 27 2021 13:56:37\n[Sun Mar 7 16:02:00 2021] [notice] Accept mutex: sysvsem (Default: sysvsem)\n[Sun Mar 7 17:21:44 2021] [info] [client xx.xx.xx.xx] (104)Connection reset by peer: client stopped connection\n[Sun Mar 7 17:23:53 2021] statistics: Use of uninitialized value in concatenation (.) or string at /home/httpd line 528.\n[Sun Mar 7 17:23:53 2021] statistics: Can't create file /home/httpd/twiki/data/Main/WebStatistics.txt - Permission denied\n[Sun Mar 7 17:27:37 2021] [info] [client xx.xx.xx.xx] (104)Connection reset by peer: client stopped connection\n[Sun Mar 7 17:31:39 2021] [info] [client xx.xx.xx.xx] (104)Connection reset by peer: client stopped connection\n[Sun Mar 7 21:16:17 2021] [error] [client xx.xx.xx.xx] File does not exist: /home/httpd/twiki/view/Main/WebHome"
                },
                new
                {
                    type = "markdown",
                    value = "`customLang` 中主要是 `tokens` 设置，这里是语言词法配置，它有 4 个配置项：\n- `name`：词法名称\n- `regex`：词法的正则匹配，注意因为是在字符串中，这里正则中如果遇到 `\\` 需要写成 `\\\\`\n- `regexFlags`: 可选，正则的标志参数\n- `color`：颜色\n- `fontStyle`: 可选，字体样式，比如 `bold` 代表加粗"
                }
                }
            };
            return data;
        }

        private object GetRichTextObj()
        {
            var data = new
            {
                
                            name = "html",
                            type = "input-rich-text",
                            label = "富文本",
                            value = "<p>Just do <code>IT</code></p>"
                        
            };
            return data;
        }

        private object GetInputFileObj()
        {
            var data = new
            {

                name = "file",
                type = "input-file",
                label = "文件上传",
                btnLabel= "上传文件",

            };
            return data;
        }
    }
}
