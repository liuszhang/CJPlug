using CJ.Plug.Models.Job;

using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Station;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.PlugBaseCore.Contracts
{
    public interface IToolExecuteService
    {
        Task<ExecuteResultData?> ExecuteToolAsync(PlugExecutionRequest plugExecutionRequest);
    }
}
