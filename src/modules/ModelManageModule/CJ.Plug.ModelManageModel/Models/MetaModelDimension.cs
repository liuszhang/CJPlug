namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 元模型维度 — 七维元模型架构（参考 CJOntology M0-M7）
    /// </summary>
    public enum MetaModelDimension
    {
        /// <summary>M0 — 基础语义（标签、分类、数据类型定义）</summary>
        M0 = 0,

        /// <summary>M1 — 对象结构（实体、属性、字段）</summary>
        M1 = 1,

        /// <summary>M2 — 行为（动作、事件、工作流）</summary>
        M2 = 2,

        /// <summary>M3 — 规则（约束、推导、验证）</summary>
        M3 = 3,

        /// <summary>M4 — 场景（用例、上下文、编排）</summary>
        M4 = 4,

        /// <summary>M1.5 — 属性扩展（自定义属性、元字段定义）</summary>
        M1_5 = 15,

        /// <summary>M5 — 主体（角色、权限、归属）</summary>
        M5 = 5,

        /// <summary>M5.5 — 接口契约（外部系统、API 规范）</summary>
        M5_5 = 55,

        /// <summary>M6 — 可靠性（审计、版本、追溯）</summary>
        M6 = 6,

        /// <summary>M7 — 质量（指标、评估、优化）</summary>
        M7 = 7
    }
}
