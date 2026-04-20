using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class DefaultInputMapping
    {
        public int? Id { get; set; }

        public string? InputVariableName { get; set; }

        public string? Keywords { get; set; }        
        public string? Value { get; set; }

        public bool IsValueFromOtherVariable { get; set; } = false;
        public string? SourceVariableName { get; set; } // 如果IsValueFromOtherVariable为true，则此字段表示来源变量的名称
    }
}
