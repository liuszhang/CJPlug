using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.VariableType
{
    public class WordTextMapping
    {
        public int? Id { get; set; }
        //书签名称
        public string? BMName { get; set; }
        //要插入书签的内容
        public string? InputSchema { get; set; }
        public bool? NeedEval { get; set; }
    }
}
