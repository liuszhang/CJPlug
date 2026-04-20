using CJ.Plug.VariableUIHandler.Models;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.VariableUIHandler.Contracts
{
    public interface IVariableTypeHandler
    {
        bool GetHandlerOfType(string type);
        string GetTypeName();
        string GetTypeDisplayName();
        bool GetTypeBrowserable();

        RenderFragment? DisplayInputEditor(string variableType,DisplayInputEditorContext context);

        /// <summary>
        /// 当参数为数组时，展开时的子元素展示形式
        /// </summary>
        /// <param name="variableType"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        RenderFragment? GetArrayChildContent(string variableType, DisplayInputEditorContext context);

    }
}
