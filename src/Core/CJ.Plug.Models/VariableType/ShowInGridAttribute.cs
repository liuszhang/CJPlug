using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    // 自定义属性，用于标记需要输出的字段
    [AttributeUsage(AttributeTargets.Property)]
    [Obsolete]
    public class ShowInGridAttribute : Attribute
    {
    }
}
