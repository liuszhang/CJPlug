using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.Models.Shared
{
    public class UserRole : IdentityRole<int>
    {
        //public int Id { get; set; }
        //public string? Name { get; set; }
        public string? RoleType { get; set; }  //分为系统角色和团队角色
        public string? Description { get; set; }

    }
}
