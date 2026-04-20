using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType.TypeSettings
{
    public static class VariableTypeShowBinding
    {
        // 定义一个静态字典来存储类型是否显示可绑定至其他参数的按钮
        private static readonly Dictionary<string, bool> TypeShowBindingSetting = new Dictionary<string, bool>
        {
            { VariableTypeEnum.String.ToString(), true },
            { VariableTypeEnum.Float.ToString(), true },
            { VariableTypeEnum.Int.ToString(), true },
            { VariableTypeEnum.Bool.ToString(), true },
            { VariableTypeEnum.File.ToString(), true },
            { VariableTypeEnum.TextMapping.ToString(), false },
            { VariableTypeEnum.ModelParameters.ToString(), false },
            { VariableTypeEnum.RequestHeader.ToString(), false },
            { VariableTypeEnum.DefaultOutputMapping.ToString(), false },
            { VariableTypeEnum.DefaultInputMapping.ToString(), false },
            { VariableTypeEnum.ToolCommandVariable.ToString(), false },
            { VariableTypeEnum.ToolVariable.ToString(), true },
            { VariableTypeEnum.WordTextMapping.ToString(), false },
            { VariableTypeEnum.ConditionExpression.ToString(), false },
        };

        public static bool GetTypeShowBinding(string type)
        {
            if (TypeShowBindingSetting.TryGetValue(type, out bool browsable))
            {
                return browsable;
            }
            // 如果不存在，返回false
            return false;
        }
    }
}
