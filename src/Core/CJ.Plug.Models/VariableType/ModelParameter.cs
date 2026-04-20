using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class ModelParameter:BaseVariable
    {
        public string? SettingValue { get; set; } 
        public string? OutToVariable { get; set; } //输出到变量
    }
}
