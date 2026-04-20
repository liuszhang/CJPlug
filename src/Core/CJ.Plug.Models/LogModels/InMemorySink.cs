using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.LogModels
{
    public class InMemorySink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
            // Format the log event as a string and add it to the global collector.
            var renderedMessage = logEvent.RenderMessage(); // Note: This method does not exist in Serilog; you need to implement your own formatting.
            GlobalLogCollector.AddMessage(renderedMessage);
        }
    }
}
