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
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    }
}
