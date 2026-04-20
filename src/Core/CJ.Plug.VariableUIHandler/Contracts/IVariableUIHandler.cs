using CJ.Plug.VariableUIHandler.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Contracts
{
    public interface IVariableUIHandler
    {
        /// <summary>
        /// Returns true if the handler supports the specified UI hint.
        /// </summary>
        bool GetSupportsUIHint(string uiHint);

        /// <summary>
        /// Returns the UI syntax for the handler.
        /// </summary>
        //string UISyntax { get; }

        /// <summary>
        /// Returns a <see cref="RenderFragment"/> that renders the input editor.
        /// </summary>
        Task<RenderFragment?> DisplayInputEditor(DisplayInputEditorContext context);

        /// <summary>
        /// 当参数为数组时，展开时的子元素展示形式
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        RenderFragment? GetArrayChildContent(DisplayInputEditorContext context);
    }
}
