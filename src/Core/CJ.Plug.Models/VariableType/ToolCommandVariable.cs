using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class ToolCommandVariable
    {
        public string VariableName { get; set; }
        public string? VariableValue { get; set; }
        public string? VariableType { get; set; } // 例如：String, Int, Bool等,暂定由工具json文件指定
        public string? VariableDescription { get; set; } // 变量描述
        public bool IsBrowsable { get; set; } = true; // 是否可浏览
        public bool IsRequired { get; set; } = false; // 是否必填
        public bool IsFromPlugVariable { get; set; } = false; // 是否来自插头变量
        public string? SourcePlugDefinitionId { get; set; } // 如果来自插头变量，记录来源插头的DefinitionId
        public string? SourcePlugName { get; set; } // 如果来自插头变量，记录来源插头的名称
        public string? PDZId { get; set; } // 数据空间ID，用于标识变量所属的数据空间
    }
}
