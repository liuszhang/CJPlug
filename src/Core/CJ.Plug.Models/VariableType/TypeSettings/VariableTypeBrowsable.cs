using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType.TypeSettings
{
    public static class VariableTypeBrowsable
    {
        // 定义一个静态字典来存储类型是否对用户可见的设置
        private static readonly Dictionary<string, bool> TypeBrowsableSetting = new Dictionary<string, bool>
        {
            { VariableTypeEnum.String.ToString(), true },
            { VariableTypeEnum.Float.ToString(), true },
            { VariableTypeEnum.Int.ToString(), true },
            { VariableTypeEnum.Bool.ToString(), true },
            { VariableTypeEnum.File.ToString(), true },
            { VariableTypeEnum.TextMapping.ToString(), true },
            { VariableTypeEnum.ModelParameters.ToString(), true },
            { VariableTypeEnum.RequestHeader.ToString(), true },
            { VariableTypeEnum.DefaultOutputMapping.ToString(), true },
            { VariableTypeEnum.DefaultInputMapping.ToString(), true },
            { VariableTypeEnum.ToolCommandVariable.ToString(), true },
            { VariableTypeEnum.ToolVariable.ToString(), true },
            { VariableTypeEnum.WordTextMapping.ToString(), true },
            { VariableTypeEnum.ConditionExpression.ToString(), true },
        };

        public static bool GetTypeBrowsable(string type)
        {
            if (TypeBrowsableSetting.TryGetValue(type, out bool browsable))
            {
                return browsable;
            }
            // 如果不存在，返回false
            return true;
        }
    }
}
