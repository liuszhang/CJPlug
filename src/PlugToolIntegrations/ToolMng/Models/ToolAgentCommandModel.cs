namespace ToolMng.Models
{
    public class ToolAgentCommandModel
    {
        public string ToolName { get; set; }
        public string ToolVersion { get; set; } = string.Empty;
        public string ExecuteCommand { get; set; } = string.Empty;
        public string[] ExecuteParameters { get; set; }
        public string RequestId { get; set; } = string.Empty;
    }
}
