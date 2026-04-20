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
    public class RichTextGuiItemService : IGuiItemService
    {
        public bool IsThisGuiItem(string itemName)=> itemName == "RichText";


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
            return GetRichTextObj();
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

    }
}
