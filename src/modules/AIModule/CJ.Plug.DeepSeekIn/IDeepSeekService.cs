using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.DeekSeekIn
{
    public interface IDeepSeekService
    {
        IAsyncEnumerable<string> Ask(string Question);
        IAsyncEnumerable<string> AskWithTool(string Question);
        IAsyncEnumerable<string> StreamReasoningFromContentAsync(string content);
    }
}
