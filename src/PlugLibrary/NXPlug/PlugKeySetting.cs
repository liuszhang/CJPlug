using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NXPlug
{
    /// <summary>
    /// NX 插头项目的密钥设置
    /// 使用嵌套静态类按插头类型组织密钥，避免命名冲突
    /// </summary>
    public static class PlugKeySetting
    {
        // ========================= NXPlug (主插头) 密钥 =========================
        public static class NXPlug
        {
            /// <summary>通用设置页面密钥</summary>
            public static string CommonSettingPageKey = "NXPlug";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "NXPlug";
            /// <summary>动作执行密钥 - NXGetParameters</summary>
            public static string ActionExecuteKey = "NXGetParameters";
            /// <summary>动作执行密钥 - NXSetParameters</summary>
            public static string ActionExecuteKey2 = "NXSetParameters";
            /// <summary>动作执行密钥 - NXToStl</summary>
            public static string ActionExecuteKey3 = "NXToStl";
        }
        
        // ========================= NXGetParameters (获取参数) 密钥 =========================
        public static class NXGetParameters
        {
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "NXGetParametersPlug";
        }
        
        // ========================= NXSetParameters (设置参数) 密钥 =========================
        public static class NXSetParameters
        {
            /// <summary>通用设置页面密钥</summary>
            public static string CommonSettingPageKey = "NXSetParametersPlug";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "NXSetParametersPlug";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "NXSetParametersPlug";
        }
        
        // ========================= NXToStl (模型转STL) 密钥 =========================
        public static class NXToStl
        {
            /// <summary>通用设置页面密钥</summary>
            public static string CommonSettingPageKey = "NXToStl";
            /// <summary>通用执行密钥</summary>
            public static string CommonExecuteKey = "NXToStl";
            /// <summary>动作执行密钥</summary>
            public static string ActionExecuteKey = "NXToStl";
        }
    }
}
