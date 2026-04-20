using System;
using System.Collections.Generic;
using System.Text;

namespace CJ.Plug.Models.Shared
{
    public class FileDeleteRequest
    {
        public string PlugDataZoneId { get; set; } = string.Empty;
        public string? PlugDefinitionId { get; set; }
        public string? FilePath { get; set; }
        public string? FileName { get; set; } =string.Empty;
    }
}
