using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType.TypeSettings
{
    public static class VariableTypeConverter
    {
        // 定义一个静态字典来存储类型和显示名称的映射关系
        private static readonly Dictionary<string, string> TypeDisplayNames = new Dictionary<string, string>
    {
        { VariableTypeEnum.String.ToString(), "字符串" },
        { VariableTypeEnum.Float.ToString(), "浮点数" },
        { VariableTypeEnum.Int.ToString(), "整数" },
        { VariableTypeEnum.Bool.ToString(), "布尔值" },
        { VariableTypeEnum.File.ToString(), "文件" },
        { VariableTypeEnum.TextMapping.ToString(), "文本映射" },
        { VariableTypeEnum.ModelParameters.ToString(), "模型参数" },
        { VariableTypeEnum.RequestHeader.ToString(), "请求头" },
        { VariableTypeEnum.DefaultOutputMapping.ToString(), "输出清单" },
        { VariableTypeEnum.DefaultInputMapping.ToString(), "输入映射" },
        { VariableTypeEnum.ToolCommandVariable.ToString(), "工具命令参数" },
        { VariableTypeEnum.ToolVariable.ToString(), "工具" },
        { VariableTypeEnum.WordTextMapping.ToString(), "书签映射" },
        { VariableTypeEnum.ConditionExpression.ToString(), "条件表达式" },
    };

        public static string GetTypeDisplayName(string type)
        {
            // 检查传入的类型是否存在于字典中
            if (TypeDisplayNames.TryGetValue(type, out string displayName))
            {
                return displayName;
            }
            // 如果不存在，返回原始类型
            return type;
        }
    }
}
