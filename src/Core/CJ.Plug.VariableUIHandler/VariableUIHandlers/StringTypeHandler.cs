using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.VariableUIHandlers
{
    public class StringTypeHandler : IVariableTypeHandler
    {
        public bool GetHandlerOfType(string type) => type == "string";
        public string GetTypeName() => "string";
        public string GetTypeDisplayName() => "字符串";
        public bool GetTypeBrowserable() => true;
        public RenderFragment? DisplayInputEditor(string variableType, DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
                builder.CloseElement();
            };
        }
        public RenderFragment? GetArrayChildContent(string variableType, DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "text");
                builder.CloseElement();
            };
        }
    }
}
