using CJ.Plug.VariableUIHandler.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IPlugExecuteHandlerService
    {
        IPlugCommonExecute? GetExecuteHandler(string? PlugTypeKey);
        IPlugCommonExecute? GetCategoryFallbackHandler(string? category);
        /// <summary>
        /// 获取所有已注册的 Category 回退处理器，用于 Category 未知时的最终兜底查找。
        /// </summary>
        IEnumerable<IPlugCategoryFallbackHandler> GetAllCategoryFallbackHandlers();
    }
}
