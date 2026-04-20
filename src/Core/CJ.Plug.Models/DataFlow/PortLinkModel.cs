using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.DataFlow
{
    public class PortLinkModel
    {
        public PortIdentifierModel? SourcePort { get; set; }
        public PortIdentifierModel? TargetPort { get; set; }
    }
}
