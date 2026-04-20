using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class DefaultOutputMapping
    {
        public int? Id { get; set; }

        public string? OutputName { get; set; }

        public string? Keywords { get; set; }        
        public string? ReadSchemaValue { get; set; } //读取数据的schema值
        public string? Value { get; set; }
    }
}
