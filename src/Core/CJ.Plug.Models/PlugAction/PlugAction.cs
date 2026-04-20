using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.PlugAction
{
    public class PlugAction :Plug.Plug
    {
        public int? ParentPlugId { get; set; }
        public string? ParentPlugDefinitionId { get; set; }
        public string? ParentPlugVersion { get; set; }

        //public string? ActionName { get; set; }
        //用于查找额外配置界面的动作唯一Key，需要手动配置
        //public string? ActionKey { get; set; }
        public string? TargetLib { get; set; }
        public string? TargetLibPath { get; set; }
        public string? LibFunctionName { get; set; }
        public string? FunctionParameters { get; set; }

        
        public bool SupportNoWindow { get; set; } = true;
        public bool NeedToolEnvirment { get; set; } = true;

        public string? ExecutingString { get; set; }

        //取消活动参数，直接使用插头参数
        //public List<ToolActionVariable>? ToolActionVariables { get; set; }=new List<ToolActionVariable>();
        //保存插头执行动作的设定，以json格式保存
        //public string? ToolActionExecutingSetting { get; set; }//取消，用插头配置来代替
        //public bool IsExecutingAction { get; set; } = false;//取消，用关系表来判断
    }
}
