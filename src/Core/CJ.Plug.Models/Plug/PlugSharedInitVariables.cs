using CJ.Plug.Models.Shared;
using CJ.Plug.Models.VariableType;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Plug
{
    public class PlugSharedInitVariables
    {
        public static List<PlugVariable> GetInitVariables()
        {
            var InitVariables = new List<PlugVariable>();
            //InitVariables.Add(new PlugVariable()
            //{
            //    Name = "OnlyExecuteAction",
            //    Type = VariableTypeEnum.Bool.ToString(),
            //    Value = "false",
            //    Description = "标识是否只执行插头动作",
            //    IsBrowsable = false,
            //});
            return InitVariables;
        }
    }
}
