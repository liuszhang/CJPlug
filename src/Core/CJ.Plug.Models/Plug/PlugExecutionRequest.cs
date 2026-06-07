using CJ.Plug.Models.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


    public class PlugExecutionRequest
    {
        public string? PlugType { get; set; }  //插头类型，标识执行的插头类型
        public string? PlugTypeKey { get; set; }  //插头类型Key，标识执行的插头类型Key


        public string? ToolName { get; set; }  //执行工具
        public string? ToolVersion { get; set; }
        public string? ToolFullPath { get; set; }  //执行工具路径，可能会根据不同图站获取特定配置路径，用于图站从服务端下载后存放的路径
        public string? RequestCommand { get; set; }  //执行参数,根据工具执行命令进行替换生成的真实执行参数

        //手动指定的图站IP地址,可以在启动流程时指定具体的IP，如果为空，则执行时会自动根据调度系统分配IP进行执行
        public string? SpecifiedStationIp { get; set; }  

    public ExecuteMode? ExecuteMode { get; set; }

    /// <summary>StationApiServer 端口号，StationAgent 执行完成后回传结果使用。默认 7660</summary>
    public int StationApiPort { get; set; } = 7660;

        /// <summary>MCP 调用时的工具类型: "Workflow" 或 "Plugin"，用于 StartExecutePlug 内部路由</summary>
        public string? McpToolType { get; set; }

    //执行的输入参数列表，用于将值替换至RequestCommand中以生成真实执行的RequestCommand
    public List<PlugVariableData> InputVariables { get; set; } = new();


    //插头执行阶段：标识前处理、执行过程、后处理
    //public ExecuteStep? ExecuteStep { get; set; } = Models.Plug.ExecuteStep.Execute;


    //执行结果数据，包含执行结果、执行状态、执行时间等信息,用于后处理阶段的结果处理
    public ExecuteResultData? ExecuteResultData { get; set; } = new ExecuteResultData();
        public string? PDZId
        {
            get
            {
                return this.ExecuteResultData?.Ids?.PDZId;
            }
            set
            {
                this.ExecuteResultData.Ids.PDZId= value;
            }
        }
        public string? PlugDefinitionId
        {
            get
            {
                return this.ExecuteResultData?.Ids?.PlugDefinitionId;
            }
            set
            {
                this.ExecuteResultData.Ids.PlugDefinitionId=value;
            }
        }


    }

    public enum ExecuteMode
    {
        Plug,//作为插头执行
        Action,//作为动作执行，数据来源于父插头
        Standalone //独立执行，数据来源于request中的输入，执行时等待结果输出，一般用于设计过程的动作执行数据获取
    }

    //public enum ExecuteStep
    //{
    //    PreProcess,//前处理
    //    Execute,//执行
    //    PostProcess//后处理
    //}

