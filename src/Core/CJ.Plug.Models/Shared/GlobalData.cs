using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CJ.Plug.Models.Shared
{
    public class GlobalData
    {
        public static string MainDispatcherServer = "http://localhost:8686";
        public static string MainApiServer = "http://localhost:8687";

        /// <summary>主服务器上记录的最新图站部署包版本号</summary>
        public static string StationVersion = "0.2.0";
        public static string ElsaEngineServer = "http://localhost:5001";
        public static string ElsaEngineApiKey = "00000000-0000-0000-0000-000000000000";
        public static string MainWebFileServerUrl = "http://localhost:5066";

        public static string MainFileServerPathRoot = "C://tmp//FileServer";
        public static string MainWebFileServer = "C://tmp//Web";

        public static string StationFileRootPath = Path.Combine("C:", "tmp", "StationTmpFiles");

        // 工具根路径
        public static string ToolsRootPath => Path.Combine(MainFileServerPathRoot, "Tools");
        // 系统工具路径（所有人可见）
        public static string SystemToolsPath => Path.Combine(ToolsRootPath, "0System");
        // 获取用户工具路径
        public static string GetUserToolsPath(string userName) => Path.Combine(ToolsRootPath, userName);
        // Plugs 根路径（与 Tools、Skills、PDZs 平级）
        public static string PlugsRootPath => Path.Combine(MainFileServerPathRoot, "Plugs");
        // 获取用户 Plugs 路径
        public static string GetUserPlugsPath(string userName) => Path.Combine(PlugsRootPath, userName);
        // PDZ 根路径
        public static string PDZsRootPath => Path.Combine(MainFileServerPathRoot, "PDZs");
        // 获取用户 PDZ 路径
        public static string GetUserPDZPath(string userName) => Path.Combine(PDZsRootPath, userName);
    }
}
