using CJ.Plug.PlugBaseCore.Contracts;
using CJ.Plug.VariableUIHandler.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Services
{
    public class PlugExecuteHandlerService : IPlugExecuteHandlerService
    {
        private readonly IEnumerable<IPlugCommonExecute> _handlers;

        public PlugExecuteHandlerService(IEnumerable<IPlugCommonExecute> handlers)
        {
            _handlers = handlers;
        }

        public IPlugCommonExecute? GetExecuteHandler(string? PlugTypeKey)
        {
            if (string.IsNullOrEmpty(PlugTypeKey))
                return null;

            return _handlers.FirstOrDefault(x => x.IsThisPlugTypeKey(PlugTypeKey));
        }

        public IPlugCommonExecute? GetCategoryFallbackHandler(string? category)
        {
            if (string.IsNullOrEmpty(category))
                return null;

            return _handlers
                .OfType<IPlugCategoryFallbackHandler>()
                .FirstOrDefault(h => h.Category == category) as IPlugCommonExecute;
        }

        public IEnumerable<IPlugCategoryFallbackHandler> GetAllCategoryFallbackHandlers()
        {
            return _handlers.OfType<IPlugCategoryFallbackHandler>();
        }
    }

}
