using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner.Contracts
{
    public interface IGuiItemHandlerService
    {
        /// <summary>
        /// 获取指定itemName的处理器
        /// </summary>
        /// <param name="itemName"></param>
        /// <returns></returns>
        IGuiItemService GetGuiHandler(string itemName);
        /// <summary>
        /// 获取所有支持的UIHint列表
        /// </summary>
        /// <returns></returns>
        List<IGuiItemService> GetAllGuiItems();
    }
}
