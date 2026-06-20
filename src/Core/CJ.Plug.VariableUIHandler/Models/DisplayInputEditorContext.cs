
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Markdown;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Models
{
    public class DisplayInputEditorContext
    {
        //当前插头对象（Plug 定义，在定义编辑模式下可用；PDZ 模式下可为 null）
        public Plug.Models.Plug.Plug? Plug { get; set; }
        //当前插头ID
        public string? PlugDefinitionId { get; set; }
        //当前参数对象
        public BaseVariable? Variable { get; set; }
        //完整PDZ数据（PDZ 模式下可用，定义模式下为 null）
        public PlugDataZone? PlugDataZone { get; set; }
        //值变更回调，由 VariableValueUI 注入，用于通知父组件（如 PlugVariableDataGrid）触发保存
        public Func<Task>? OnValueChanged { get; set; }
    }
}
