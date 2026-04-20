using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class Tool
    {
        public int? Id { get; set; }
        public string? ToolName { get; set; }
        public string? ToolPath { get; set; }
        public string? ToolVersion { get; set; } = "1.0";
        public string? CommandParameter { get; set; } = "[ToolPath] ";
        public string? ToolDescription { get; set; }
        public string? ToolCompany { get; set; } = "CJ";
        public string? ToolType { get; set; }=ToolTypeEnum.桌面类_商业.ToString();
        public string? ToolLocation { get; set; }= ToolLocationEnum.图站.ToString();
        public bool IsEnabled { get; set; }= true;
        public bool? IsSystemInitTool { get; set; }=false;
        public bool? IsBrowsable { get; set; }= true;

        public List<BaseVariable> GetVariablesFromToolCommand()
        {
            if (CommandParameter == null ||
                string.IsNullOrWhiteSpace(CommandParameter))
            {
                return new List<BaseVariable>();
            }

            var variables = new List<BaseVariable>();
            var startIndex = 0;

            while (startIndex < CommandParameter.Length)
            {
                // 查找下一个 '['
                var openBracketIndex = CommandParameter.IndexOf('[', startIndex);
                if (openBracketIndex == -1) break;

                // 查找对应的 ']'
                var closeBracketIndex = CommandParameter.IndexOf(']', openBracketIndex + 1);
                if (closeBracketIndex == -1) break;

                // 提取括号内的参数名称并去除空格
                var paramName = CommandParameter.Substring(openBracketIndex + 1, closeBracketIndex - openBracketIndex - 1).Trim();
                if (!string.IsNullOrEmpty(paramName))
                {
                    variables.Add(new BaseVariable { Name = paramName });
                }

                // 移动到下一个可能的 '['
                startIndex = closeBracketIndex + 1;
            }

            return variables;
        }
    }
}
