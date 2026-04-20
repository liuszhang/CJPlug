using CJ.Plug.Models.VariableType;
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
    public class ModelParameterTypeHandler : IVariableUIHandler
    {
        public bool GetSupportsUIHint(string uiHint)
        {
            return uiHint == VariableTypeEnum.ModelParameters.ToString();
        }
        //public string UISyntax => "File";
        public async Task<RenderFragment?> DisplayInputEditor(DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenComponent<MudAlert>(0);
                builder.AddAttribute(1, nameof(MudAlert.Severity), Severity.Warning);
                builder.AddAttribute(2, nameof(MudAlert.Class), "my-2");
                builder.AddAttribute(3, nameof(MudAlert.Dense), true);
                builder.AddAttribute(4, nameof(MudText.ChildContent), (RenderFragment)(textBuilder =>
                {
                    textBuilder.AddContent(0, $"请在组件中设置");
                }));
                builder.CloseComponent();
            };
        }

        public RenderFragment? GetArrayChildContent(DisplayInputEditorContext context)
        {
            return builder =>
            {
                builder.OpenComponent(0, typeof(ModelParameterTable));
                builder.AddAttribute(1, nameof(ModelParameterTable.EditorContext), context);
                builder.CloseComponent();
            };
        }
    }
}
