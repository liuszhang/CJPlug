using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ExecuteResultData
    {
        public ExecuteIdsBundle? Ids { get; set; } = new();
        //执行结果状态
        public JobStatus? ExecuteStatus { get; set; }= JobStatus.执行中;
        //执行结果子状态
        public JobSubStatus? ExecuteSubStatus { get; set; } = JobSubStatus.提交;
        //执行结果额外扩展描述，比如失败时记录失败代码等，可以为空
        public string? ExecuteResultMessage { get; set; }
        //执行输出端口，用于流程引擎获取并判定后续流程走向
        public string[]? Outcome { get; set; } = ["Done"];

        //执行结果字符串
        public string? ResultString { get; set; }
        //执行结果文件列表
        public List<FileInformation>? ResultFileInformations { get; set; }
    }
}
