using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CJ.Plug.Models.Knowledge
{
    /// <summary>
    /// 知识条目实体
    /// </summary>
    public class KnowledgeItem
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属文件夹ID
        /// </summary>
        public int FolderId { get; set; }

        /// <summary>
        /// 知识条目标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 知识内容（P0 阶段纯文本）
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 标签，逗号分隔字符串（如 "C#,Blazor,教程"）
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// 排序顺序，默认 0
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 是否启用，默认 true
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建者
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// 所属文件夹导航属性
        /// </summary>
        [ForeignKey(nameof(FolderId))]
        public KnowledgeFolder? Folder { get; set; }
    }
}
