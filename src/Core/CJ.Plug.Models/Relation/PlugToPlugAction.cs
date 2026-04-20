using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Relation
{
    public class PlugToPlugAction
    {
        public int? Id { get; set; }

        public int? PlugId { get; set; }
        public int? PlugActionId { get; set; }
        public string? PlugDefinitionId { get; set; }
        public string? PlugActionDefinitionId { get; set; }
        public string? PlugToolVersion { get; set; }
        //定义该条关系时初始化是创建的还是需要执行的动作关系
        public string? RelationshipType { get; set; }
    }
}
