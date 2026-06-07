using System;
using System.ComponentModel.DataAnnotations;

namespace CJ.Plug.Models.Skills
{
    public class Skill
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? PromptTemplate { get; set; }
        public bool IsEnabled { get; set; } = true;
        /// <summary>
        /// 是否为系统预设技能。系统预设技能不允许删除。
        /// </summary>
        public bool IsPreset { get; set; } = false;
        public string? Category { get; set; }
        public string? Icon { get; set; }
        public string? Author { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? SourcePlugId { get; set; }
    }
}