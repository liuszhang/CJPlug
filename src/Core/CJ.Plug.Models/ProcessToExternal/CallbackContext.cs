using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.ProcessToExternal
{
    public class CallbackContext
    {
        public string? Url { get; set; }
        public string? Method { get; set; }
        public string? Data { get; set; }
    }
}
