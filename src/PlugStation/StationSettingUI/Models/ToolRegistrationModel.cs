using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StationSettingUI.Models
{
    public class ToolRegistrationModel
    {
        public int Id { get; set; }
        public string? ToolName { get; set; }
        public string? ToolVersion { get; set; }
        public string? ToolPath { get; set; }
        public string? ToolCommand { get; set; }
        public bool SetupStatus { get; set; }= false;
    }
}
