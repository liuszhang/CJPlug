using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CJ.Plug.Models.Knowledge
{
    /// <summary>
    /// 知识库实体，作为知识管理的顶层容器
    /// </summary>
    public class KnowledgeBase
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 知识库名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 知识库描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 知识库图标（Base64 图片，如 data:image/png;base64,...）
        /// </summary>
        public string? IconUrl { get; set; }

        /// <summary>
        /// 排序顺序，默认 0
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 知识库下的文件夹列表
        /// </summary>
        public List<KnowledgeFolder> Folders { get; set; } = new();
    }
}
