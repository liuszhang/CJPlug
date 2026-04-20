using CJ.Plug.Models.Plug;
using CJ.Plug.GUIDesigner.Models.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner.Contracts
{
    public interface IGuiItemService
    {
        bool IsThisGuiItem(string itemName);
        ItemDisplaySetting? GetItemDisplaySetting();
        object? ToAmisObject();


        Task<RenderFragment?> GetItemGuiContent();
        Task<RenderFragment?> GetItemPropertySettingContent();

        //插头实现基础插头配置信息

    }
}
