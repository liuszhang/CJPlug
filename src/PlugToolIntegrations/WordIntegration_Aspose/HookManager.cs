using Crane.MethodHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WordIntegration_Aspose
{
    [Flags]
    public enum HookMethods
    {
        Invoke = 1,
        ParseExact = 2,
        DateTimeOpGreaterThan = 4,
        StringCompare = 8,
        StringIndexOf = 16,
        XmlElementInnerText = 32
    }
    public class NewHookMethods
    {
        //将过期日期修改为
        private static readonly string DATE_CHANGED_TO = (DateTime.Today.Year + 1).ToString() + "1230";
        #region New hook methods
        public static object NewMethodInvoke(MethodBase method, object obj, object[] parameters)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "ParseExact" && parameters.Length > 0 && parameters[0].ToString().Contains("0827"))
            {
                var ret = DateTime.ParseExact(DATE_CHANGED_TO, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                ShowLog(method, ret, obj, parameters, true);
                return ret;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "ParseExact" && parameters.Length > 0 && System.Text.RegularExpressions.Regex.Match(parameters[0].ToString(), @"^\d{4}\.\d{2}\.\d{2}$").Success)
            {
                var ret = DateTime.ParseExact("20200501", "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                ShowLog(method, ret, obj, parameters, true);
                return ret;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "get_InnerText" && obj is XmlElement && (obj as XmlElement).Name == "SubscriptionExpiry")
            {
                ////这里不能用，否则会出错。
                //ShowLog(method, DATE_CHANGED_TO, obj, parameters, true);
                //return DATE_CHANGED_TO;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "get_Ticks" && obj is DateTime && ((DateTime)obj).ToString("MMdd") == "0827")
            {
                var ret = DateTime.ParseExact(DATE_CHANGED_TO, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture).Ticks;
                ShowLog(method, ret, obj, parameters, true);
                return ret;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.DeclaringType != null && method.DeclaringType.Name == "String" && method.Name == "Compare")
            {
                if (parameters.Length == 2)
                {
                    if (parameters[0].ToString() == "20200827")
                    {
                        var ret = 1;
                        ShowLog(method, ret, obj, parameters, true);
                        return ret;
                    }
                    else if (parameters[1].ToString() == "20200827")
                    {
                        var ret = -1;
                        ShowLog(method, ret, obj, parameters, true);
                        return ret;
                    }
                }
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "op_GreaterThan" && parameters.Length == 2 && parameters[1] is DateTime && ((DateTime)parameters[1]).ToString("MMdd") == "0827")
            {
                ShowLog(method, false, obj, parameters, true);
                return false;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "IndexOf" && parameters.Length > 0 && parameters[0].ToString().Contains("0827"))
            {
                ShowLog(method, 580, obj, parameters, true);
                return 580;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "Split" && System.Text.RegularExpressions.Regex.Match(obj.ToString(), @"^\d{4}\.\d{2}\.\d{2}$").Success
                     && obj != null && obj.ToString().Substring(0, 4) == DateTime.Now.Year.ToString())
            {
                var ret = new string[] { "2019", "08", "27" };
                ShowLog(method, ret, obj, parameters, true);
                return ret;
            }
            else if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && method.Name == "get_Now")
            {
                var ret = DateTime.ParseExact("20200518", "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                ShowLog(method, ret, obj, parameters, true);
                return ret;
            }
            var hook = MethodHookManager.Instance.GetHook(System.Reflection.MethodBase.GetCurrentMethod());
            var result = hook.InvokeOriginal<object>(method, obj, parameters?.ToArray());
            ShowLog(method, result, obj, parameters);
            return result;
        }
        /// <summary>
        /// 获取类型
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private static string Serialize(object obj)
        {
            if (obj is string) return obj?.ToString();
            if (obj is byte[])
            {
                return "byte[]";
            }
            if (obj is Stream)
            {
                return "<STREAM>...";
            }
            if (obj is IEnumerable<object>)
            {
                var arr = obj as IEnumerable<object>;
                if (arr.Any())
                {
                    return "[" + (arr.Aggregate("", (a, b) => (a == "" ? "" : Serialize(a) + ",") + Serialize(b))) + "]";
                }
                else
                {
                    return "[]";
                }
            }
            else if (obj is char[])
            {
                return string.Join(",", obj as char[]);
            }
            else
            {
                return obj?.ToString();
            }
        }
        /// <summary>
        /// 输出日志
        /// </summary>
        /// <param name="method"></param>
        /// <param name="ret"></param>
        /// <param name="obj"></param>
        /// <param name="parameters"></param>
        /// <param name="isImportant"></param>
        public static void ShowLog(MethodBase method, object ret, object obj, object[] parameters, bool isImportant = false)
        {
            try
            {
                if (isImportant == false)
                {
                    // 只拦截字符串类型的和时间类型的元素
                    if ((method.DeclaringType?.Name.Contains("DateTime") == false
                         && method.DeclaringType?.Name.Contains("String.") == false)
                        || method.Name.Contains("ToCharArray")
                        || method.Name.Contains("IndexOf")
                        || method.Name.Contains("get_Length")
                        || method.Name.Contains("op_Equality")
                        || method.Name.Contains("ToLower"))
                    {
                        //Utils.LogWriteLine($"Bob Is Not Important {method.DeclaringType?.Name}.{method.Name}", ConsoleColor.White);
                        return;
                    }
                }
                var paras = string.Empty;
                try
                {
                    paras = Serialize(obj) + "," + Serialize(parameters);
                }
                catch (Exception)
                {
                    paras = obj + "," + parameters;
                }
                var returns = string.Empty;
                try
                {
                    returns = Serialize(ret);
                }
                catch (Exception)
                {
                    returns = ret?.ToString();
                }
                Utils.LogWriteLine($"INVOKE method {method.DeclaringType?.Name}.{method.Name}({paras}) RETURN=> {returns}", isImportant ? ConsoleColor.Blue : ConsoleColor.DarkGray);
            }
            catch (Exception e)
            {
                Utils.LogWrite("Error:" + e.Message);
            }
        }
        public static int NewCompare(string s1, string s2)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && s2 == "20200827")
            {
                Utils.LogWriteLine($"HOOK SUCCESS: From {Assembly.GetCallingAssembly().GetName().Name} String.Compare({s1},{s2}) return -1;", ConsoleColor.Green);
                return -1;
            }
            else
            {
                var hook = MethodHookManager.Instance.GetHook(MethodBase.GetCurrentMethod());
                var ret = hook.InvokeOriginal<int>(null, s1, s2);
                Utils.LogWriteLine($"NOT Aspose Call: From {Assembly.GetCallingAssembly().GetName().Name} String.Compare({s1},{s2}) return {ret};", ConsoleColor.DarkRed);
                return ret;
            }
        }
        public static bool NewGreaterThan(DateTime t1, DateTime t2)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && t2.ToString("yyyyMMdd") == "20200827")
            {
                Utils.LogWriteLine($"HOOK SUCCESS: From {Assembly.GetCallingAssembly().GetName().Name} DateTime ({t1}>{t2}) return false;", ConsoleColor.Green);
                return false;
            }
            else
            {
                var hook = MethodHookManager.Instance.GetHook(MethodBase.GetCurrentMethod());
                var ret = hook.InvokeOriginal<bool>(null, t1, t2);
                return ret;
            }
        }
        public static DateTime NewParseExact(string s, string format, IFormatProvider provider)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && s == "20200827")
            {
                Utils.LogWriteLine($"HOOK SUCCESS: From {Assembly.GetCallingAssembly().GetName().Name} DateTime.ParseExact({s},{format},{provider}) return {DATE_CHANGED_TO};", ConsoleColor.Green);
                var hook = MethodHookManager.Instance.GetHook(System.Reflection.MethodBase.GetCurrentMethod());
                return hook.InvokeOriginal<DateTime>(null, DATE_CHANGED_TO, format, provider);
            }
            else
            {
                var hook = MethodHookManager.Instance.GetHook(System.Reflection.MethodBase.GetCurrentMethod());
                return hook.InvokeOriginal<DateTime>(null, s, format, provider);
            }
        }
        public static string NewInnerText(XmlElement element)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.Words") == false && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.Hook") == false && element.Name == "SubscriptionExpiry")
            {
                Utils.LogWriteLine($"HOOK SUCCESS: From {Assembly.GetCallingAssembly().GetName().Name} XmlElement.InnerText ({element.Name},{element.InnerXml}) return {DATE_CHANGED_TO};", ConsoleColor.Green);
                return DATE_CHANGED_TO;
            }
            else
            {
                var hook = MethodHookManager.Instance.GetHook(System.Reflection.MethodBase.GetCurrentMethod());
                return hook.InvokeOriginal<string>(element);
            }
        }
        public static int NewIndexOf(string v1, string v2)
        {
            if (Assembly.GetCallingAssembly() != null && Assembly.GetCallingAssembly().FullName.StartsWith("Aspose.") && v2 == DATE_CHANGED_TO)
            {
                Utils.LogWriteLine($"HOOK SUCCESS: From {Assembly.GetCallingAssembly().GetName().Name} {v1.ToString().Substring(0, 9) + "..."}.IndexOf({v2}) return 580;", ConsoleColor.Green);
                return 580;
            }
            else
            {
                var hook = MethodHookManager.Instance.GetHook(System.Reflection.MethodBase.GetCurrentMethod());
                var ret = hook.InvokeOriginal<int>(v1, v2);
                return ret;
            }
        }
        #endregion
    }
    public static class HookManager
    {
        private class HookItem
        {
            public HookMethods Method { get; set; }
            public MethodHook Hook { get; set; }
            public bool Enabled { get; set; }
        }
        //所有的license都只需要设置一次
        private static List<string> mAssembliesLicenseSetted = new List<string>();
        //将于20200827过期的Aspose.Total开发版序列号
        private const string LICENSE_STRING = "PExpY2Vuc2U+CiAgPERhdGE+CiAgICA8TGljZW5zZWRUbz5TdXpob3UgQXVuYm94IFNvZnR3YXJlIENvLiwgT" +
                                              "HRkLjwvTGljZW5zZWRUbz4KICAgIDxFbWFpbFRvPnNhbGVzQGF1bnRlYy5jb208L0VtYWlsVG8+CiAgICA8TG" +
                                              "ljZW5zZVR5cGU+RGV2ZWxvcGVyIE9FTTwvTGljZW5zZVR5cGU+CiAgICA8TGljZW5zZU5vdGU+TGltaXRlZCB" +
                                              "0byAxIGRldmVsb3BlciwgdW5saW1pdGVkIHBoeXNpY2FsIGxvY2F0aW9uczwvTGljZW5zZU5vdGU+CiAgICA8" +
                                              "T3JkZXJJRD4xOTA4MjYwODA3NTM8L09yZGVySUQ+CiAgICA8VXNlcklEPjEzNDk3NjAwNjwvVXNlcklEPgogI" +
                                              "CAgPE9FTT5UaGlzIGlzIGEgcmVkaXN0cmlidXRhYmxlIGxpY2Vuc2U8L09FTT4KICAgIDxQcm9kdWN0cz4KIC" +
                                              "AgICAgPFByb2R1Y3Q+QXNwb3NlLlRvdGFsIGZvciAuTkVUPC9Qcm9kdWN0PgogICAgPC9Qcm9kdWN0cz4KICA" +
                                              "gIDxFZGl0aW9uVHlwZT5FbnRlcnByaXNlPC9FZGl0aW9uVHlwZT4KICAgIDxTZXJpYWxOdW1iZXI+M2U0NGRl" +
                                              "MzAtZmNkMi00MTA2LWIzNWQtNDZjNmEzNzE1ZmMyPC9TZXJpYWxOdW1iZXI+CiAgICA8U3Vic2NyaXB0aW9uR" +
                                              "XhwaXJ5PjIwMjAwODI3PC9TdWJzY3JpcHRpb25FeHBpcnk+CiAgICA8TGljZW5zZVZlcnNpb24+My4wPC9MaW" +
                                              "NlbnNlVmVyc2lvbj4KICAgIDxMaWNlbnNlSW5zdHJ1Y3Rpb25zPmh0dHBzOi8vcHVyY2hhc2UuYXNwb3NlLmN" +
                                              "vbS9wb2xpY2llcy91c2UtbGljZW5zZTwvTGljZW5zZUluc3RydWN0aW9ucz4KICA8L0RhdGE+CiAgPFNpZ25h" +
                                              "dHVyZT53UGJtNUt3ZTYvRFZXWFNIY1o4d2FiVEFQQXlSR0pEOGI3L00zVkV4YWZpQnd5U2h3YWtrNGI5N2c2e" +
                                              "GtnTjhtbUFGY3J0c0cwd1ZDcnp6MytVYk9iQjRYUndTZWxsTFdXeXNDL0haTDNpN01SMC9jZUFxaVZFOU0rWn" +
                                              "dOQkR4RnlRbE9uYTFQajhQMzhzR1grQ3ZsemJLZFZPZXk1S3A2dDN5c0dqYWtaL1E9PC9TaWduYXR1cmU+CjwvTGljZW5zZT4=";
        private static List<HookItem> mHookStatus = new List<HookItem>();
        private static bool mHookStarted = false;
        static HookManager()
        {
        }
        static void InitializeHookList()
        {
            if (mHookStatus.Count == 0)
            {
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.Invoke,
                    Hook = new MethodHook(
                        typeof(MethodBase).GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(object), typeof(object[]) }, null),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewMethodInvoke), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(MethodBase), typeof(object), typeof(object[]) }, null)
                    ),
                    Enabled = true
                });
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.ParseExact,
                    Hook = new MethodHook(
                        typeof(DateTime).GetMethod("ParseExact", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string), typeof(IFormatProvider) }, null),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewParseExact), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string), typeof(IFormatProvider) }, null)
                    ),
                    Enabled = true
                });
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.DateTimeOpGreaterThan,
                    Hook = new MethodHook(
                        typeof(DateTime).GetMethod("op_GreaterThan", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(DateTime), typeof(DateTime) }, null),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewGreaterThan), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(DateTime), typeof(DateTime) }, null)
                    ),
                    Enabled = true
                });
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.StringCompare,
                    Hook = new MethodHook(
                        typeof(string).GetMethod("Compare", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewCompare), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null)
                    ),
                    Enabled = true
                });
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.StringIndexOf,
                    Hook = new MethodHook(
                        typeof(string).GetMethod("IndexOf", BindingFlags.Instance | BindingFlags.Public, null, new[] { typeof(string) }, null),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewIndexOf), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(string), typeof(string) }, null)
                    ),
                    Enabled = true
                });
                mHookStatus.Add(new HookItem
                {
                    Method = HookMethods.XmlElementInnerText,
#if NET40
                    Hook = new MethodHook(
                        typeof(XmlElement).GetProperty("InnerText", BindingFlags.Public | BindingFlags.Instance).GetGetMethod(true),
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewInnerText), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(XmlElement) }, null)
                    ),
#else
                    Hook = new MethodHook(
                        typeof(XmlElement).GetProperty("InnerText", BindingFlags.Public | BindingFlags.Instance).GetMethod,
                        typeof(NewHookMethods).GetMethod(nameof(NewHookMethods.NewInnerText), BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(XmlElement) }, null)
                    ),
#endif
                    Enabled = true
                });
                foreach (var item in mHookStatus)
                {
                    if (item.Enabled)
                    {
                        MethodHookManager.Instance.AddHook(item.Hook);
                    }
                }
            }
        }
        /// <summary>
        /// 设置需要启用的方法清单（默认已启用了所有）
        /// </summary>
        /// <param name="methods"></param>
        public static void SetHookMethods(HookMethods methods)
        {
            InitializeHookList();
            MethodHookManager.Instance.StopHook();
            MethodHookManager.Instance.RemoveAllHook();
            foreach (var item in mHookStatus)
            {
                if (methods.HasFlag(item.Method))
                {
                    item.Enabled = true;
                    MethodHookManager.Instance.AddHook(item.Hook);
                }
                else
                {
                    item.Enabled = false;
                }
            }
            if (mHookStarted)
            {
                MethodHookManager.Instance.StartHook();
            }
        }
        /// <summary>
        /// 是否显示详细的HOOK明细
        /// </summary>
        /// <param name="show"></param>
        public static void ShowHookDetails(bool show)
        {
            if (show)
            {
                Utils.EnableLog();
            }
            else
            {
                Utils.DisableLog();
            }
        }
        /// <summary>
        /// 启用hook
        /// </summary>
        public static void StartHook()
        {
            if (mHookStarted)
            {
                return;
            }
            try
            {
                InitializeHookList();
                MethodHookManager.Instance.StartHook();
                //所有库的License都只需要设置一次就可以了。
                var assemblies = Assembly.GetCallingAssembly()?.GetReferencedAssemblies().Union(Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? new AssemblyName[] { })
                    .GroupBy(assembly => assembly.Name).Select(item => item.FirstOrDefault())
                    .Where(assembly => assembly.Name.StartsWith("Aspose") && assembly.Name.StartsWith("Aspose.Hook") == false);
                if (assemblies != null)
                {
                    foreach (var assembly in assemblies)
                    {
                        if (mAssembliesLicenseSetted.Contains(assembly.FullName))
                        {
                            continue;
                        }
                        else
                        {
                            var type = Assembly.Load(assembly).GetType(assembly.Name + ".License");
                            if (type == null)
                            {
                                type = Assembly.Load(assembly).GetType(System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(assembly.Name.ToLower()) + ".License");
                            }
                            if (type != null)
                            {
                                Utils.LogWriteLine($"\nSETTING...{type.FullName}", ConsoleColor.Yellow);
                                var instance = Activator.CreateInstance(type);
                                type.GetMethod("SetLicense", new[] { typeof(Stream) }).Invoke(instance, BindingFlags.Public | BindingFlags.Instance, null, new[] { new MemoryStream(Convert.FromBase64String(LICENSE_STRING)) }, null);
                                Utils.LogWriteLine($"{type.FullName} SET SUCCESSFULLY.", ConsoleColor.Yellow);
                                mAssembliesLicenseSetted.Add(assembly.FullName);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                var exception = e;
                while (exception.InnerException != null)
                {
                    exception = exception.InnerException;
                }
                Utils.LogWriteLine($"start hook failed because of {exception.Message}.", ConsoleColor.Red);
            }
            mHookStarted = true;
        }
        /// <summary>
        /// 停用Hook
        /// </summary>
        public static void StopHook()
        {
            if (mHookStarted == false)
            {
                return;
            }
            MethodHookManager.Instance.StopHook();
            mHookStarted = false;
        }
    }
}
