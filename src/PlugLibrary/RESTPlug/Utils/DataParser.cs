using CJ.Plug.Models.LogModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace RESTPlug.Utils
{
    public class DataParser
    {
        /// <summary>
        /// 使用解析表达式从目标 JSON 字符串中提取值并返回解析结果。
        /// </summary>
        /// <param name="TargetString">解析表达式（例如: $.a.b[0].c 或 a/b[0]/c）</param>
        /// <param name="MappingString">目标 JSON 字符串</param>
        /// <returns>解析得到的值的字符串表示，出错或未找到返回空字符串</returns>
        public static string GetParsedResult(string TargetString, string MappingString)
        {
            CLog.Information($"GetParsedResult called with MappingString: {MappingString}");
            try
            {
                if (string.IsNullOrWhiteSpace(TargetString) || string.IsNullOrWhiteSpace(MappingString))
                    return string.Empty;

                // Determine which parameter contains JSON and which is the path expression.
                static bool LooksLikeJson(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return false;
                    var t = s.Trim();
                    if (t.Length == 0) return false;
                    char c = t[0];
                    if (c == '{') return true;
                    if (c == '[')
                    {
                        // treat as JSON array only if it contains JSON-like tokens (quotes, objects or colons)
                        if (t.IndexOf('"') >= 0 || t.IndexOf('{') >= 0 || t.IndexOf(':') >= 0) return true;
                        return false; // likely an expression like [0]
                    }
                    if (c == '"' || c == '-' || char.IsDigit(c)) return true;
                    if (t.StartsWith("true", StringComparison.OrdinalIgnoreCase) || t.StartsWith("false", StringComparison.OrdinalIgnoreCase) || t.StartsWith("null", StringComparison.OrdinalIgnoreCase))
                        return true;
                    return false;
                }

                string expr = TargetString.Trim();
                string jsonCandidate = MappingString;
                if (!LooksLikeJson(jsonCandidate) && LooksLikeJson(expr))
                {
                    // parameters were likely passed in swapped order
                    jsonCandidate = TargetString;
                    expr = MappingString.Trim();
                }

                JsonNode document = null;
                // Try to parse the chosen JSON candidate; if that fails, try the other parameter.
                string otherCandidate = (jsonCandidate == MappingString) ? TargetString : MappingString;
                static string? ExtractTopLevelJson(string s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return null;
                    int len = s.Length;
                    int i = 0;
                    // find first '{' or '['
                    while (i < len && char.IsWhiteSpace(s[i])) i++;
                    if (i >= len) return null;
                    if (s[i] != '{' && s[i] != '[') return null;
                    char open = s[i];
                    char close = open == '{' ? '}' : ']';
                    int depth = 0;
                    bool inString = false;
                    for (int j = i; j < len; j++)
                    {
                        char c = s[j];
                        if (c == '"')
                        {
                            // toggle inString unless escaped
                            bool escaped = false;
                            int k = j - 1;
                            while (k >= i && s[k] == '\\') { escaped = !escaped; k--; }
                            if (!escaped) inString = !inString;
                        }
                        if (inString) continue;
                        if (c == open) depth++;
                        else if (c == close)
                        {
                            depth--;
                            if (depth == 0)
                            {
                                // return substring from i to j inclusive
                                return s.Substring(i, j - i + 1);
                            }
                        }
                    }
                    return null;
                }

                bool parsed = false;
                // try primary candidate
                try
                {
                    document = JsonNode.Parse(jsonCandidate);
                    parsed = true;
                }
                catch (System.Text.Json.JsonException)
                {
                    // try extracting top-level JSON from the candidate (handles cases like '{...}/path')
                    var prefix = ExtractTopLevelJson(jsonCandidate);
                    if (!string.IsNullOrEmpty(prefix))
                    {
                        try { document = JsonNode.Parse(prefix); parsed = true; }
                        catch { parsed = false; }
                    }
                }

                if (!parsed)
                {
                    // try the other candidate
                    try
                    {
                        document = JsonNode.Parse(otherCandidate);
                        expr = (jsonCandidate == MappingString) ? TargetString.Trim() : MappingString.Trim();
                        parsed = true;
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        var prefix2 = ExtractTopLevelJson(otherCandidate);
                        if (!string.IsNullOrEmpty(prefix2))
                        {
                            try { document = JsonNode.Parse(prefix2); parsed = true; expr = (jsonCandidate == MappingString) ? TargetString.Trim() : MappingString.Trim(); }
                            catch { parsed = false; }
                        }
                    }
                }

                if (!parsed || document == null)
                {
                    // Fallback: if parsing failed but the candidate looks like plain text (no object/array),
                    // treat it as a primitive string value so expressions like "[0]" can index into it.
                    bool plainTextFallback = false;
                    string rawCandidate = jsonCandidate;
                    if (string.IsNullOrWhiteSpace(rawCandidate)) rawCandidate = otherCandidate;
                    if (!string.IsNullOrWhiteSpace(rawCandidate) && rawCandidate.IndexOfAny(new char[] { '{', '[' }) == -1)
                    {
                        document = JsonValue.Create(rawCandidate.Trim());
                        parsed = true;
                        plainTextFallback = true;
                    }

                    if (!parsed || document == null)
                    {
                        CLog.Error($"GetParsedResult: failed to parse JSON from inputs.");
                        return string.Empty;
                    }
                }

                // Tokenize the expression. Support: $, dot (.), slash (/), and brackets [index]
                expr = expr.Trim();
                if (expr.StartsWith("$")) expr = expr.Substring(1);
                while (expr.Length > 0 && (expr[0] == '.' || expr[0] == '/')) expr = expr.Substring(1);

                var tokens = new List<string>();
                var sb = new StringBuilder();
                for (int i = 0; i < expr.Length; i++)
                {
                    char c = expr[i];
                    if (c == '.' || c == '/')
                    {
                        if (sb.Length > 0)
                        {
                            tokens.Add(sb.ToString().Trim());
                            sb.Clear();
                        }
                    }
                    else if (c == '[')
                    {
                        if (sb.Length > 0)
                        {
                            tokens.Add(sb.ToString().Trim());
                            sb.Clear();
                        }
                        int j = i + 1;
                        var idxSb = new StringBuilder();
                        while (j < expr.Length && expr[j] != ']')
                        {
                            idxSb.Append(expr[j]);
                            j++;
                        }
                        if (j >= expr.Length)
                            throw new FormatException("Missing closing ']' in expression.");
                        // strip surrounding quotes inside brackets, and trim
                        var idxTok = idxSb.ToString().Trim();
                        if ((idxTok.StartsWith("\"") && idxTok.EndsWith("\"")) || (idxTok.StartsWith("'") && idxTok.EndsWith("'")))
                            idxTok = idxTok.Substring(1, idxTok.Length - 2);
                        tokens.Add(idxTok);
                        i = j; // skip to ']'
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                if (sb.Length > 0) tokens.Add(sb.ToString());

                CLog.Information($"GetParsedResult tokens: {string.Join(',', tokens)}");
                foreach (var rawToken in tokens)
                {
                    if (document == null)
                        return string.Empty;

                    var token = rawToken?.Trim();
                    if (string.IsNullOrEmpty(token)) continue;
                    // strip surrounding quotes
                    if ((token.StartsWith("\"") && token.EndsWith("\"")) || (token.StartsWith("'") && token.EndsWith("'")))
                        token = token.Substring(1, token.Length - 2);

                    if (int.TryParse(token, out int index))
                    {
                        // treat as array index when current node is an array
                        if (document is JsonArray arr)
                        {
                            if (index < 0 || index >= arr.Count) return string.Empty;
                            document = arr[index];
                        }
                        else if (document is JsonObject obj)
                        {
                            // numeric property name on object
                            document = TryGetPropertyIgnoreCase(obj, token);
                        }
                        else if (document is JsonValue primitiveValue)
                        {
                            // allow indexing into a string primitive, e.g. "abc"[0] -> "a"
                            var val = primitiveValue.GetValue<object>();
                            if (val is string s)
                            {
                                if (index < 0 || index >= s.Length) return string.Empty;
                                document = JsonValue.Create(s[index].ToString());
                            }
                            else
                            {
                                // cannot index into other primitive types
                                return string.Empty;
                            }
                        }
                        else
                        {
                            // cannot index into a primitive value
                            return string.Empty;
                        }
                    }
                    else
                    {
                        if (document is JsonObject obj)
                        {
                            document = TryGetPropertyIgnoreCase(obj, token);
                            if (document == null)
                            {
                                CLog.Information($"GetParsedResult: property '{token}' not found on object.");
                                return string.Empty;
                            }
                        }
                        else if (document is JsonValue primitiveValue)
                        {
                            // if it's a string, try to parse it as JSON then access the property
                            var val = primitiveValue.GetValue<object>();
                            if (val is string s)
                            {
                                try
                                {
                                    var inner = JsonNode.Parse(s);
                                    if (inner is JsonObject innerObj)
                                    {
                                        document = innerObj[token];
                                    }
                                    else
                                    {
                                        return string.Empty;
                                    }
                                }
                                catch (System.Text.Json.JsonException)
                                {
                                    return string.Empty;
                                }
                            }
                            else
                            {
                                // cannot get string-named property from non-object
                                return string.Empty;
                            }
                        }
                        else
                        {
                            // cannot get string-named property from non-object
                            return string.Empty;
                        }
                    }
                }

                if (document == null) return string.Empty;

                if (document is JsonValue jv)
                {
                    var val = jv.GetValue<object>();
                    if (val == null) return "null";
                    return val.ToString() ?? string.Empty;
                }

                // For objects/arrays return JSON string
                return document.ToJsonString();
            }
            catch (Exception ex)
            {
                CLog.Error(ex.Message);
                CLog.Error(ex.StackTrace);
                return string.Empty;
            }
        }

        private static JsonNode? TryGetPropertyIgnoreCase(JsonObject obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName)) return null;
            if (obj.ContainsKey(propertyName)) return obj[propertyName];
            // try case-insensitive match by iterating entries (JsonObject doesn't expose Keys)
            foreach (var kvp in obj)
            {
                if (string.Equals(kvp.Key, propertyName, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return null;
        }
    }
}
