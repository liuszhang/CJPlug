using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatiaPlug
{
    /// <summary>
    /// Catia 插头项目的密钥设置
    /// 使用嵌套静态类按插头类型组织密钥，避免命名冲突
    /// </summary>
    public static class PlugKeySetting
    {
        // ========================= CatiaPlug (主插头) 密钥 =========================
        public static class CatiaPlug
        {
            /// <summary>通用设置页面密钥</summary>
            public static string CommonSettingPageKey = "CatiaPlug";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "CatiaPlug";
            /// <summary>动作执行密钥 - CatiaGetParameters</summary>
            public static string ActionExecuteKey = "CatiaGetParameters";
            /// <summary>动作执行密钥 - CatiaSetParameters</summary>
            public static string ActionExecuteKey2 = "CatiaSetParameters";
            /// <summary>动作执行密钥 - CatiaExportStp</summary>
            public static string ActionExecuteKey3 = "CatiaExportStp";
            /// <summary>动作执行密钥 - CatiaExportStl</summary>
            public static string ActionExecuteKey4 = "CatiaExportStl";
        }

        // ========================= CatiaGetParameters (获取参数) 密钥 =========================
        public static class CatiaGetParameters
        {
            /// <summary>通用设置页面密钥（与子插头 PlugTypeKey 保持一致，参照 NX 对齐）</summary>
            public static string CommonSettingPageKey = "CatiaGetParameters";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "CatiaGetParameters";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "CatiaGetParameters";
        }

        // ========================= CatiaSetParameters (设置参数) 密钥 =========================
        public static class CatiaSetParameters
        {
            /// <summary>通用设置页面密钥（与子插头 PlugTypeKey 保持一致，参照 NX 对齐）</summary>
            public static string CommonSettingPageKey = "CatiaSetParameters";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "CatiaSetParameters";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "CatiaSetParameters";
        }

        // ========================= CatiaExportStp (导出STP) 密钥 =========================
        public static class CatiaExportStp
        {
            /// <summary>通用设置页面密钥（与子插头 PlugTypeKey 保持一致，参照 NX 对齐）</summary>
            public static string CommonSettingPageKey = "CatiaExportStp";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "CatiaExportStp";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "CatiaExportStp";
        }

        // ========================= CatiaExportStl (导出STL) 密钥 =========================
        public static class CatiaExportStl
        {
            /// <summary>通用设置页面密钥（与子插头 PlugTypeKey 保持一致，参照 NX 对齐）</summary>
            public static string CommonSettingPageKey = "CatiaExportStl";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "CatiaExportStl";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "CatiaExportStl";
        }
    }
}
