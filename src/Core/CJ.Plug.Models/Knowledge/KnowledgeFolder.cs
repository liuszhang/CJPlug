using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CJ.Plug.Models.Knowledge
{
    /// <summary>
    /// 知识文件夹实体，支持自关联多级嵌套
    /// </summary>
    public class KnowledgeFolder
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// 所属知识库ID
        /// </summary>
        public int BaseId { get; set; }

        /// <summary>
        /// 文件夹名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父文件夹ID，null 表示根级文件夹
        /// </summary>
        public int? ParentId { get; set; }

        /// <summary>
        /// 文件夹描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹图标
        /// </summary>
        public string Icon { get; set; } = string.Empty;

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
        /// 所属知识库导航属性
        /// </summary>
        [ForeignKey(nameof(BaseId))]
        public KnowledgeBase? Base { get; set; }

        /// <summary>
        /// 父文件夹导航属性
        /// </summary>
        [ForeignKey(nameof(ParentId))]
        public KnowledgeFolder? Parent { get; set; }

        /// <summary>
        /// 子文件夹导航属性
        /// </summary>
        public List<KnowledgeFolder> Children { get; set; } = new();

        /// <summary>
        /// 文件夹下的知识条目
        /// </summary>
        public List<KnowledgeItem> Items { get; set; } = new();
    }
}
