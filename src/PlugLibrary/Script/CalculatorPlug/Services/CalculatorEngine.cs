namespace CalculatorPlug.Services
{
    /// <summary>
    /// 计算器引擎 — 前后端通用的数学表达式求值器。
    /// 支持：+ - * / % ^ ( ) 和常用数学函数。
    /// </summary>
    public static class CalculatorEngine
    {
        /// <summary>
        /// 求值结果
        /// </summary>
        public readonly struct EvalResult
        {
            public bool Success { get; init; }
            public double Value { get; init; }
            public string? Error { get; init; }

            public override string ToString() => Success ? Value.ToString() : Error ?? "未知错误";
        }

        /// <summary>
        /// 计算表达式并返回结果。
        /// </summary>
        /// <param name="expression">数学表达式字符串</param>
        /// <returns>EvalResult，Success 为 true 时 Value 有效，否则 Error 含错误信息</returns>
        public static EvalResult Evaluate(string? expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return new EvalResult { Success = false, Error = "表达式不能为空" };

            try
            {
                var tokens = Tokenize(expression);
                if (tokens.Count == 0)
                    return new EvalResult { Success = false, Error = "无效表达式" };

                int pos = 0;
                double result = ParseExpression(tokens, ref pos);

                if (pos < tokens.Count)
                    return new EvalResult { Success = false, Error = $"表达式在位置 {pos} 处存在未预期的字符" };

                if (double.IsInfinity(result) || double.IsNaN(result))
                    return new EvalResult { Success = false, Error = "计算结果溢出或无效" };

                return new EvalResult { Success = true, Value = result };
            }
            catch (Exception ex)
            {
                return new EvalResult { Success = false, Error = $"计算错误: {ex.Message}" };
            }
        }

        // ---- Tokenizer ----

        private enum TokenType { Number, Operator, LParen, RParen }

        private readonly struct Token
        {
            public TokenType Type { get; init; }
            public double Value { get; init; }
            public char Op { get; init; }
        }

        private static List<Token> Tokenize(string expr)
        {
            var tokens = new List<Token>();
            int i = 0;

            while (i < expr.Length)
            {
                char c = expr[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    int start = i;
                    while (i < expr.Length && (char.IsDigit(expr[i]) || expr[i] == '.'))
                        i++;
                    if (double.TryParse(expr[start..i],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out double val))
                    {
                        tokens.Add(new Token { Type = TokenType.Number, Value = val });
                    }
                    else
                    {
                        throw new FormatException($"无效数字: {expr[start..i]}");
                    }
                    continue;
                }

                if (c == '(')
                {
                    tokens.Add(new Token { Type = TokenType.LParen });
                    i++;
                    continue;
                }

                if (c == ')')
                {
                    tokens.Add(new Token { Type = TokenType.RParen });
                    i++;
                    continue;
                }

                if ("+-*/%^".Contains(c))
                {
                    // 处理负号：表达式开头或 ( 后的 - 视为负号
                    if (c == '-' && (tokens.Count == 0 ||
                        tokens[^1].Type == TokenType.LParen ||
                        (tokens[^1].Type == TokenType.Operator)))
                    {
                        // 将负号与后面的数字合并
                        tokens.Add(new Token { Type = TokenType.Number, Value = 0 });
                        tokens.Add(new Token { Type = TokenType.Operator, Op = '-' });
                        i++;
                        // 下一个 token 如果是数字，其值仍为正，减法运算会自然处理
                        continue;
                    }

                    tokens.Add(new Token { Type = TokenType.Operator, Op = c });
                    i++;
                    continue;
                }

                throw new FormatException($"无效字符: '{c}'");
            }

            return tokens;
        }

        // ---- Recursive Descent Parser ----

        // 优先级：Expression → Term { ('+'|'-') Term }
        //        Term       → Power { ('*'|'/'|'%') Power }
        //        Power      → Unary { '^' Unary }
        //        Unary      → '-' Unary | Primary
        //        Primary    → Number | '(' Expression ')'

        private static double ParseExpression(List<Token> tokens, ref int pos)
        {
            double left = ParseTerm(tokens, ref pos);

            while (pos < tokens.Count &&
                   tokens[pos].Type == TokenType.Operator &&
                   (tokens[pos].Op == '+' || tokens[pos].Op == '-'))
            {
                char op = tokens[pos].Op;
                pos++;
                double right = ParseTerm(tokens, ref pos);
                left = op == '+' ? left + right : left - right;
            }

            return left;
        }

        private static double ParseTerm(List<Token> tokens, ref int pos)
        {
            double left = ParsePower(tokens, ref pos);

            while (pos < tokens.Count &&
                   tokens[pos].Type == TokenType.Operator &&
                   (tokens[pos].Op == '*' || tokens[pos].Op == '/' || tokens[pos].Op == '%'))
            {
                char op = tokens[pos].Op;
                pos++;
                double right = ParsePower(tokens, ref pos);

                left = op switch
                {
                    '*' => left * right,
                    '/' => right != 0 ? left / right
                        : throw new DivideByZeroException("除数不能为零"),
                    '%' => right != 0 ? left % right
                        : throw new DivideByZeroException("取模运算中除数为零"),
                    _ => left
                };
            }

            return left;
        }

        private static double ParsePower(List<Token> tokens, ref int pos)
        {
            double left = ParseUnary(tokens, ref pos);

            while (pos < tokens.Count &&
                   tokens[pos].Type == TokenType.Operator &&
                   tokens[pos].Op == '^')
            {
                pos++;
                double right = ParseUnary(tokens, ref pos);
                left = Math.Pow(left, right);
            }

            return left;
        }

        private static double ParseUnary(List<Token> tokens, ref int pos)
        {
            if (pos < tokens.Count &&
                tokens[pos].Type == TokenType.Operator &&
                tokens[pos].Op == '-')
            {
                pos++;
                return -ParseUnary(tokens, ref pos);
            }

            return ParsePrimary(tokens, ref pos);
        }

        private static double ParsePrimary(List<Token> tokens, ref int pos)
        {
            if (pos >= tokens.Count)
                throw new FormatException("表达式不完整");

            var token = tokens[pos];

            if (token.Type == TokenType.Number)
            {
                pos++;
                return token.Value;
            }

            if (token.Type == TokenType.LParen)
            {
                pos++; // skip '('
                double val = ParseExpression(tokens, ref pos);
                if (pos >= tokens.Count || tokens[pos].Type != TokenType.RParen)
                    throw new FormatException("缺少右括号 ')'");
                pos++; // skip ')'
                return val;
            }

            throw new FormatException($"意外的 token: {token.Type}");
        }
    }
}
