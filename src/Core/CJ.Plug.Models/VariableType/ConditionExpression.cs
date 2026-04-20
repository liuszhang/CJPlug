using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class ConditionExpression
    {
        public string? PlugDefinitionId { get; set; }
        public string? PlugName { get; set; }
        public string? VariableName { get; set; }
        public int? VariableId { get; set; }
        public string? Expression { get; set; }="==";
        public string? ConditionValue { get; set; } = "";
        public bool IsValueFromVariable { get; set; } = false;
        public string? SourceVariableName { get; set; } = "";
    }
}
