using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.Models.Shared
{
    public class Department
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Code { get; set; }
        public int? ParentId { get; set; }
        public string? ParentName { get; set; }
        public string? Manager { get; set; }
        /// <summary>
        /// 状态：-1=授权中，0=禁用，1=启用
        /// </summary>
        public int Status { get; set; } = 1;
        public string? Creator { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
