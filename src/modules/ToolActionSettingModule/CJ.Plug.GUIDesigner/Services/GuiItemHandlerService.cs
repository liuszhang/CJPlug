using CJ.Plug.VariableUIHandler.Contracts;
using CJ.Plug.VariableUIHandler.VariableUIHandlers;
using CJ.Plug.GUIDesigner.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CJ.Plug.GUIDesigner.Services
{
    public class GuiItemHandlerService(IEnumerable<IGuiItemService> handlers) : IGuiItemHandlerService
    {
        public List<IGuiItemService> GetAllGuiItems()
        {
            return handlers.ToList();
        }

        public IGuiItemService GetGuiHandler(string itemName)
        {
            var handler = handlers.FirstOrDefault(x => x.IsThisGuiItem(itemName));
            return handler ?? new BaseGuiItemService();
        }
    }
}
