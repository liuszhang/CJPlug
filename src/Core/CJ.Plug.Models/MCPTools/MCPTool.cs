using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.Models.MCPTools
{
    public class MCPTool
    {
        public int Id { get; set; }
        public string? ToolId { get; set; }
        public string? SourcePlugId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }=true;
        public string? Version { get; set; }
        public string? Author { get; set; }
        public string? Category { get; set; }
        public string? GroupName { get; set; }
        public string? Type { get; set; }
        public string? ToolSettingsJson { get; set; }
        /// <summary>工具类型: "Workflow"（工作流）或 "Plugin"（单插头）</summary>
        public string? ToolType { get; set; } = "Workflow";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
