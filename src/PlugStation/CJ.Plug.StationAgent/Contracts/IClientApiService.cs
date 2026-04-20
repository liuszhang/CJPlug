using CJ.Plug.Models.Job;
using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.StationAgent.Contracts
{
    public interface IClientApiService
    {
        Task SendLog(object logString,string? logLevel="Information");
        Task SendResult(PlugExecutionRequest executeRequest, string? resultString, JobSubStatus? status);
    }
}
