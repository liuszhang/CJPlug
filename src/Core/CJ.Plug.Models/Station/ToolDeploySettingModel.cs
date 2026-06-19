using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class ToolDeploySettingModel
    {
        public bool IsDeployed { get; set; } = true;

        public bool UseDefaultToolPath { get; set; } = true;
        public string? SpecialToolPath { get; set; }
        public bool AlwaysDownloadToStation { get; set; } = false;
    }
}
