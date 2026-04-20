using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Job
{
    public enum JobSubStatus
    {
        执行中,
        循环中,
        //
        // 摘要:
        //     The workflow is currently suspended and waiting for external stimuli to resume.
        Suspended,
        已完成,
        已取消,
        出错,
        等待执行,
        提交,
        等待用户输入,
        已提交至图站,
        图站执行完成,
        准备执行后处理
    }
}
