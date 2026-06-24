namespace CJ.Plug.ModelManageModel.Models
{
    /// <summary>
    /// 行为动作类型（参考 CJOntology MetaAction.ActionType）
    /// </summary>
    public enum ActionType
    {
        /// <summary>表单提交</summary>
        Submit = 0,

        /// <summary>按钮触发</summary>
        Button = 1,

        /// <summary>重置表单</summary>
        Reset = 2,

        /// <summary>页面导航</summary>
        Navigate = 3,

        /// <summary>自定义脚本</summary>
        Custom = 4
    }
}
