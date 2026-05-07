public partial class PlugDataZone
    {
        public int Id { get; set; }
        public string? PDZId { get; set; } //业务ID，UserName + ProcessDefinitionId+Type+JobInstanceId

        public string? UserName { get; set; }
        public string? PlugDefinitionId { get; set; }  //PDZ的原始插头ID，通常为流程的插头ID


        public string? Type { get; set; } = PDZTypeEnum.Desi.ToString();
        //作业唯一识别号
        public string? JobDefinitionId { get; set; }
        //流程执行时的触发活动ID
        public string? TriggerPlugDefinitionId { get; set; }

        //关联PDZ的主插头，一般为流程
        //用户名，用以区分不同用户的数据
        //保存PDZ的工作目录，用于存放和访问实体文件
        public string? PDZWorkPath { get; set; }

        


        //保存插头数据控件的完整参数信息，包括文件类型的参数
        public List<PDZVariable>? PDZVariables { get; set; } = new List<PDZVariable>();

        //将PDZVariable拆分为不同的类型，便于后续扩展和管理
        public List<PlugData>? PlugDatas { get; set; } = new List<PlugData>();
        public List<PlugVariableData>? PlugVariableDatas { get; set; } = new List<PlugVariableData>();
        public List<PlugStatusData>? PlugStatusDatas { get; set; } = new List<PlugStatusData>();
        public List<ActionData>? ActionDatas { get; set; } = new List<ActionData>();
        public List<ActionVariableData>? ActionVariableDatas { get; set; } = new List<ActionVariableData>();

        [Obsolete("流程图数据已迁移至插头定义层（Plug.ActivityJsonData），请使用 Plug.GetFlowchartJson() 代替。")]
        public List<FlowchartData>? FlowchartDatas { get; set; } = new List<FlowchartData>();

        public List<DataFlowData>? DataFlowDatas { get; set; } = new List<DataFlowData>();



        //保存分散的结果文件信息
        //public List<FileInformation>? FileInformations { get; set; } = new List<FileInformation>();


        //当前PDZ的插头动作及动作数据
        //public List<PlugActionData>? PlugActionDatas { get; set; } = new();

    }

