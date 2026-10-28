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

        /// <summary>
        /// 单窗口 VNC 目标 PID 已就绪（程序已启动并绑定，前端此时再打开可视化页面）
        /// </summary>
        VncPidReady,

        /// <summary>
        /// 单窗口 VNC 目标进程已退出 (用于自动关闭可视化窗口)
        /// </summary>
        VncWindowClosed,
    }
}
