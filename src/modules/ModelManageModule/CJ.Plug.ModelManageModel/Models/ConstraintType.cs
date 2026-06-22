namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 属性约束类型
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>字符串长度限制</summary>
        StringLength,

        /// <summary>最小值</summary>
        Min,

        /// <summary>最大值</summary>
        Max,

        /// <summary>正则表达式匹配</summary>
        Pattern,

        /// <summary>必填（比 Property.IsRequired 更细粒度）</summary>
        Required,

        /// <summary>邮箱格式</summary>
        Email,

        /// <summary>URL 格式</summary>
        Url
    }
}
