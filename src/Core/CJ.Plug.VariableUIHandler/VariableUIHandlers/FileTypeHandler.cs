using CJ.Plug.Models.VariableType;
using CJ.Plug.VariableUIHandler.Components;
using CJ.Plug.VariableUIHandler.Components.ArrayDataTable;
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
    public class FileTypeHandler : IVariableUIHandler
    {
        public bool GetSupportsUIHint(string uiHint)
        {
            return uiHint == VariableTypeEnum.File.ToString();
        }
        //public string UISyntax => "File";
        public async Task<RenderFragment?> DisplayInputEditor(DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(FileUpload));
                builder.AddAttribute(1, nameof(FileUpload.EditorContext), context);
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
