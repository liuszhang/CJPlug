using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.ProcessToExternal
{
    public class ProcessStatus
    {
        public string? ProcessId { get; set; }

        public DateTime? StartTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string? Status { get; set; }
        public string? SubStatus { get; set; }
    }
}
