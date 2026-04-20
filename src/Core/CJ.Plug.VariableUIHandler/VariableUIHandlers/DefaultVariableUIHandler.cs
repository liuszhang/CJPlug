using CJ.Plug.VariableUIHandler.Components;
using CJ.Plug.VariableUIHandler.Components.ArrayDataTable;
using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.VariableUIHandlers
{
    public class DefaultVariableUIHandler:IVariableUIHandler
    {
        public bool GetSupportsUIHint(string uiHint) => false;

        public string UISyntax => "Unsupported";

        public async Task<RenderFragment?> DisplayInputEditor(DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(DefaultTextField));
                builder.AddAttribute(1, nameof(DefaultTextField.EditorContext), context);
                builder.CloseComponent();
            };
        }

        public RenderFragment? GetArrayChildContent(DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(DefaultChildDataTable));
                builder.AddAttribute(1, nameof(DefaultChildDataTable.EditorContext), context);
                builder.CloseComponent();
            };
        }
    }
}
