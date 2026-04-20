using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.GUIDesigner.Models.Shared
{
    public class ItemDisplaySetting
    {
        public string? ItemName { get; set; }
        public string? ItemGroup { get; set; } // 组件分组
        public string? ItemType { get; set; } // 组件类型
        public string? ItemDisplayName { get; set; } // 组件显示名称
        public string? ItemDescription { get; set; } // 组件描述
        public string? ItemIcon { get; set; } // 组件图标
        public string? ItemColor { get; set; } // 组件颜色
    }
}
