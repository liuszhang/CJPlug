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
        容器类,
        流程
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

    

}
