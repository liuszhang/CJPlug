using System;

namespace WordIntegration_Aspose
{
    /// <summary>
    /// 钩子诊断日志助手。
    ///
    /// 背景（2026-10-05 补写）：HookManager.cs 里有 13 处 <c>Utils.LogWriteLine/LogWrite/EnableLog/DisableLog</c>
    /// 调用，但 <c>Utils</c> 这个类在整个仓库、以及工程引用的 Crane.MethodHook 包里都不存在
    /// （同源的 CJ.Plug.Models\Services\HookManager.cs 里这些调用全被注释掉了，本工程拷贝时取消了注释），
    /// 导致 CS0103 编译失败。此处按那 13 处调用的实际接口面把实现补齐，保留原本想输出的钩子诊断日志。
    ///
    /// 输出走控制台（本工具由平台以进程方式启动、stdout 会被采集），可随时用 EnableLog/DisableLog 关闭。
    /// </summary>
    public static class Utils
    {
        private static readonly object _gate = new();
        private static volatile bool _enabled = true;

        /// <summary>日志开关（默认开启）。</summary>
        public static bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>打开日志输出。</summary>
        public static void EnableLog() => _enabled = true;

        /// <summary>关闭日志输出（钩子命中时会非常频繁，正式使用建议关掉）。</summary>
        public static void DisableLog() => _enabled = false;

        /// <summary>输出一行文本（不换行），使用默认前景色。</summary>
        public static void LogWrite(string message) => Write(message, null);

        /// <summary>输出一行文本（带换行），可指定前景色。</summary>
        public static void LogWriteLine(string message, ConsoleColor color = ConsoleColor.Gray)
            => Write(message + Environment.NewLine, color);

        private static void Write(string text, ConsoleColor? color)
        {
            if (!_enabled || string.IsNullOrEmpty(text)) return;

            lock (_gate)
            {
                var restore = false;
                var old = ConsoleColor.Gray;
                try
                {
                    if (color.HasValue)
                    {
                        try
                        {
                            old = Console.ForegroundColor;
                            Console.ForegroundColor = color.Value;
                            restore = true;
                        }
                        catch
                        {
                            // 无控制台宿主 / 输出被重定向时设置颜色可能失败：忽略，继续输出文本。
                            restore = false;
                        }
                    }

                    Console.Write(text);
                }
                catch
                {
                    // 日志绝不能影响钩子主流程。
                }
                finally
                {
                    if (restore)
                    {
                        try { Console.ForegroundColor = old; } catch { /* ignore */ }
                    }
                }
            }
        }
    }
}
