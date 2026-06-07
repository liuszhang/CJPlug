namespace PatranPlug
{
    public enum InitVariableNames
    {
        /// <summary>
        /// 导入的Patran脚本文件
        /// </summary>
        ScriptFile,

        /// <summary>
        /// 关键字映射配置（JSON格式，由UI管理）
        /// </summary>
        KeywordMappingJson,

        /// <summary>
        /// 执行输出结果
        /// </summary>
        ResultString,

        /// <summary>
        /// 额外的命令行参数
        /// </summary>
        AdditionalArgs,
    }
}
