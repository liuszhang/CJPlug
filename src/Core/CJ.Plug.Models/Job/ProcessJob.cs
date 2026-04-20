using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public class ProcessJob : BaseJob
    {

        public string? ProcessDefinitionId { get; set; }

        public string? JournalData { get; set; } //从引擎获取的作业历程数据
    }

}
