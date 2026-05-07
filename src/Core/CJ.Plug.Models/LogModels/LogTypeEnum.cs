using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.LogModels
{
    public enum LogTypeEnum
    {
        PlugStatus,
        CommonLog,
        ActivityStatusNow,
        JobStatusUpdated,
        CompleteActivityContext,
        PDZUpdatedInfo,
        PlugUpdated,
        /// <summary>
        /// 图站开始执行通知 (用于触发 Guacamole 远程桌面)
        /// </summary>
        StationExecuting,
    }
}
