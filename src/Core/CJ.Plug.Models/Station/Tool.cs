using CJ.Plug.Models.Plug;
using CJ.Plug.Models.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Station
{
    public class Tool
    {
        public int? Id { get; set; }
        public string? ToolName { get; set; }
        public string? ToolPath { get; set; }

        /// <summary>
        /// 工具包根目录。
        /// 格式：Tools/{0System或userName}/{ToolName}
        /// 用于工具包的部署和文件检查，与 ToolPath（入口可执行文件）分离。
        /// </summary>
        public string? ToolBasePath { get; set; }

        public string? ToolVersion { get; set; } = "1.0";
        public string? CommandParameter { get; set; } = "[ToolPath] ";
        public string? ToolDescription { get; set; }
        public string? ToolCompany { get; set; } = "CJ";
        public string? ToolType { get; set; }=ToolTypeEnum.桌面类_商业.ToString();
        public string? ToolLocation { get; set; }= ToolLocationEnum.图站.ToString();
        /// <summary>
        /// 是否跳过下载至图站。默认 false（需要下载）。
        /// 勾选后适用于工具已在服务器本地或为纯接口调用、无需在图站部署可执行文件的场景。
        /// </summary>
        public bool SkipDownloadToStation { get; set; } = false;
        public bool IsEnabled { get; set; }= true;
        public bool IsSystemInitTool { get; set; }=false;
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
