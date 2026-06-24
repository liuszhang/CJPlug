namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 属性字段定义 — 本体对象的属性元数据（参考Palantir Properties）
    /// </summary>
    public class Property
    {
        public int Id { get; set; }

        /// <summary>所属本体ID</summary>
        public int OntologyId { get; set; }

        /// <summary>属性名称（如"编码格式"、"顶点数"等）</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>属性显示名称</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>属性编码（唯一标识，如 Encoding、VertexCount）</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>属性描述</summary>
        public string? Description { get; set; }

        /// <summary>
        /// 属性类型：Text（文本）/ Number（数字）/ Select（下拉）/ Date（日期）/ Boolean（布尔）/ JsonText（JSON文本）
        /// </summary>
        public string PropertyType { get; set; } = "Text";

        /// <summary>默认值</summary>
        public string? Value { get; set; }

        /// <summary>是否必填</summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>排序（数字越小越靠前）</summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>UI 提示（如输入框占位文本）</summary>
        public string? UIHint { get; set; }

        /// <summary>
        /// 下拉选项（JSON数组格式），仅 PropertyType=Select 时使用
        /// </summary>
        public string? SelectOptions { get; set; }

        /// <summary>验证规则（正则表达式或自定义规则）</summary>
        public string? ValidationRule { get; set; }

        /// <summary>是否在界面上可见/可编辑</summary>
        public bool IsBrowsable { get; set; } = true;

        /// <summary>字段长度限制（字符串类型）</summary>
        public int? Length { get; set; }

        /// <summary>关联数据字典编码（如关联 BasicEnum.Code）</summary>
        public string? DictCode { get; set; }

        /// <summary>属性约束列表</summary>
        public List<PropertyConstraint> Constraints { get; set; } = new();

        /// <summary>是否被父对象同名字段覆盖（UI展示时标记为不生效）</summary>
        public bool? IsOverridden { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.ToLocalTime();
    }
}
