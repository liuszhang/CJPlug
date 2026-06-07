namespace PatranPlug.Models
{
    /// <summary>
    /// Patran脚本关键字映射配置
    /// 定义脚本中的关键字与变量的映射关系
    /// </summary>
    public class PatranKeywordMapping
    {
        /// <summary>
        /// 脚本中的关键字（将被替换的文本）
        /// </summary>
        public string? Keyword { get; set; }

        /// <summary>
        /// 绑定的变量名（替换值来源）
        /// </summary>
        public string? VariableName { get; set; }

        /// <summary>
        /// 映射描述（可选）
        /// </summary>
        public string? Description { get; set; }
    }
}
