
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
        //当前插头对象
        //public Plug.Models.Plug.Plug? Plug { get; set; }
        //当前插头ID
        public string? PlugDefinitionId { get; set; }
        //当前参数对象
        public BaseVariable? Variable { get; set; }
        //完整PDZ数据
        public PlugDataZone? PlugDataZone { get; set; }
    }
}
