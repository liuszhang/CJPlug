using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    /// <summary>
    /// 插头种类枚举
    /// </summary>
    public enum PlugCategorys
    {
        桌面类,
        桌面类动作,
        接口类,
        接口类动作,
        脚本类,
        脚本类动作,
        数据库类,
        容器类,  //历史遗留项，已经将之前的容器类如while循环等归类到流程控制组件中了，后续新建的容器类插件如有需要可以直接归类到流程控制组件或者循环控制组件中
        流程,
        //流程控制组件如合并、结束流程等，上面的流程是指流程管理创建的流程或者子流程等；循环控制组件如ForEach、While等
        流程控制组件,
        循环控制组件,
        设备类,
        设备类动作
    }

    public static class PlugCategory
    {
        public static List<string> AllPlugCategorys { get; set; }= Enum.GetNames(typeof(PlugCategorys)).ToList();
        public static List<string>? GetPlugCategorys()
        {
            return AllPlugCategorys.Where(x => !x.Contains("动作"))?.ToList();
        }

        public static List<string>? GetPlugActionCategorys()
        {
            return AllPlugCategorys.Where(x => x.Contains("动作"))?.ToList();
        }

        public static bool IsPlugAction(Plug plug)
        {
            if (plug.Category == null)
            {
                return false;
            }
            return plug.Category.ToString().Contains("动作");
        }
    }

    /// <summary>
    /// 插头在管理界面按类别分组显示的辅助类。
    /// 将 Category 字符串映射到显示分组：桌面类、接口类、脚本类、设备类、未分类。
    /// </summary>
    public static class PlugCategoryGroupHelper
    {
        /// <summary>
        /// 分组显示顺序
        /// </summary>
        public static readonly string[] GroupOrder = { "桌面类", "接口类", "脚本类", "设备类", "未分类" };

        /// <summary>
        /// 将插头的 Category 字符串映射到显示分组。
        /// 桌面类/桌面类动作 → 桌面类；接口类/接口类动作 → 接口类；
        /// 脚本类/脚本类动作 → 脚本类；设备类/设备类动作/数据库类 → 设备类；其他 → 未分类。
        /// </summary>
        public static string GetDisplayGroup(string? category)
        {
            if (string.IsNullOrEmpty(category)) return "未分类";
            if (category.Contains("桌面")) return "桌面类";
            if (category.Contains("接口")) return "接口类";
            if (category.Contains("脚本")) return "脚本类";
            if (category.Contains("设备")) return "设备类";
            if (category.Contains("数据库")) return "设备类";
            return "未分类";
        }
    }



}
