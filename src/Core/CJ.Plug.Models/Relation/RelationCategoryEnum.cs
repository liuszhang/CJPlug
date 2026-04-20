using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Relation
{
    public enum RelationCategory
    {
        //关系类型定义，前面为RoleA,后面为RoleB

        UserToMarketPlug,
        PlugToPlugAction,
        ToolToStation
    }
}
