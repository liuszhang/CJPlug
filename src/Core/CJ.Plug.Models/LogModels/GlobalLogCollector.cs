using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CJ.Plug.Models.LogModels
{
    public class GlobalLogCollector
    {
        private static readonly ConcurrentQueue<string> _logMessages = new ConcurrentQueue<string>();
        public static List<string>? Logs => GetLogMessages().ToList() ?? new();

        public string? LogContent
        {
            get
            {
                var tmp = "";
                foreach (var l in GetLogMessages())
                {
                    var log = JsonSerializer.Deserialize<LogModel>(l);
                    tmp += log.Description;
                    tmp += "\n";
                }
                return tmp;
            }
            set
            {
                ClearLogs();
            }
        }


        public static void AddMessage(string message)
        {
            _logMessages.Enqueue(message);
        }
        public static void ClearLogs()
        {
            _logMessages.Clear();
        }

        public static string[] GetLogMessages()
        {
            // To prevent modification while enumerating, create a copy of the current items.
            var snapshot = new List<string>(_logMessages);
            return snapshot.ToArray();
        }

        public static string GetLogContent()
        {
            var tmp = "";
            foreach (var l in GetLogMessages())
            {
                var log = JsonSerializer.Deserialize<LogModel>(l);
                tmp += log.Description;
                tmp += "\n";
            }
            return tmp;
        }
    }
}
