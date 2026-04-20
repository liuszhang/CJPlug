using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Relation
{
    public class RelationFilter
    {
        public int? RoleAId { get; set; }
        public int? RoleBId { get; set; }
        public string? RelationCategory { get; set; }
    }
}
