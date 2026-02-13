namespace YounBot.Utils;

public static class HomoIntUtils
{
    public static string getInt(long num, string baseDigits = "114514")
    {
        var generator = new Generator(baseDigits);
        return generator.GetInt(num);
    }

    private readonly struct Expr
    {
        public Expr(string text, int precedence, string kind)
        {
            Text = text;
            Precedence = precedence;
            Kind = kind;
        }

        public string Text { get; }
        public int Precedence { get; }
        public string Kind { get; }
    }

    private sealed class Generator
    {
        private const int PrecAdd = 1;
        private const int PrecMul = 2;
        private const int PrecUnary = 3;
        private const int PrecAtom = 4;

        private readonly string baseDigits;
        private readonly long baseValue;
        private readonly long seedLimit;
        private readonly Expr baseExpr;
        private readonly Expr oneExpr;
        private readonly Expr zeroExpr;
        private readonly Expr negOneExpr;
        private readonly Expr twoExpr;
        private readonly Dictionary<long, Expr> cache;
        private readonly Dictionary<long, Expr> nonLiteralCache;
        private readonly Dictionary<long, Expr> fullCache;
        private readonly Dictionary<long, Expr> fullNonLiteralCache;
        private readonly List<long> seedValues;

        public Generator(string baseDigits)
        {
            if (!IsValidBase(baseDigits))
            {
                throw new ArgumentException("base must be a 6-digit number and cannot contain 0");
            }

            this.baseDigits = baseDigits;
            baseValue = long.Parse(baseDigits);
            seedLimit = Math.Max(baseValue * 2L, 2_000_000L);
            baseExpr = Lit(baseDigits);

            (fullCache, fullNonLiteralCache) = BuildSplitTable();
            cache = new Dictionary<long, Expr>(fullNonLiteralCache);
            nonLiteralCache = new Dictionary<long, Expr>(fullNonLiteralCache);

            var anchorExpr = PickAnchorExpr();
            oneExpr = PickFullValue(1L, Binary("/", anchorExpr, anchorExpr));
            zeroExpr = PickFullValue(0L, Binary("-", anchorExpr, anchorExpr));
            negOneExpr = PickFullValue(-1L, Neg(oneExpr));
            twoExpr = PickFullValue(2L, Binary("+", oneExpr, oneExpr));

            var baseCache = new Dictionary<long, Expr>
            {
                [-1L] = negOneExpr,
                [0L] = zeroExpr,
                [1L] = oneExpr,
                [2L] = twoExpr,
                [baseValue] = baseExpr
            };

            foreach (var pair in baseCache)
            {
                cache[pair.Key] = pair.Value;
                nonLiteralCache[pair.Key] = pair.Value;
                fullCache[pair.Key] = pair.Value;
                fullNonLiteralCache[pair.Key] = pair.Value;
            }

            seedValues = new List<long>();
            foreach (var value in cache.Keys)
            {
                if (value > 0L)
                {
                    seedValues.Add(value);
                }
            }
            seedValues.Sort();
        }

        public string GetInt(long num)
        {
            return FinalizeExpr(num, GetExpr(num)).Text;
        }

        private static bool IsValidBase(string? value)
        {
            if (value is null || value.Length != 6)
            {
                return false;
            }

            foreach (var ch in value)
            {
                if (ch < '1' || ch > '9')
                {
                    return false;
                }
            }

            return true;
        }

        private static Expr Lit(string text)
        {
            return new Expr(text, PrecAtom, "lit");
        }

        private static bool IsLiteral(Expr expr)
        {
            return expr.Kind == "lit";
        }

        private static int CountParentheses(string text)
        {
            var count = 0;
            foreach (var ch in text)
            {
                if (ch == '(')
                {
                    count++;
                }
            }
            return count;
        }

        private static bool IsBetter(Expr candidate, Expr current)
        {
            if (candidate.Text.Length != current.Text.Length)
            {
                return candidate.Text.Length < current.Text.Length;
            }

            return CountParentheses(candidate.Text) < CountParentheses(current.Text);
        }

        private static void Remember(Dictionary<long, Expr> table, long value, Expr expr)
        {
            if (!table.TryGetValue(value, out var oldExpr))
            {
                table[value] = expr;
                return;
            }

            if (IsBetter(expr, oldExpr))
            {
                table[value] = expr;
            }
        }

        private void RememberSeed(Dictionary<long, Expr> table, long value, Expr expr)
        {
            if (Math.Abs(value) > seedLimit)
            {
                return;
            }

            Remember(table, value, expr);
        }

        private Expr PickAnchorExpr()
        {
            var hasCandidate = false;
            var best = default(Expr);

            foreach (var pair in fullNonLiteralCache)
            {
                if (pair.Key == 0L)
                {
                    continue;
                }

                if (!hasCandidate || IsBetter(pair.Value, best))
                {
                    best = pair.Value;
                    hasCandidate = true;
                }
            }

            if (hasCandidate)
            {
                return best;
            }

            var zero = Binary("-", baseExpr, baseExpr);
            return Binary("+", baseExpr, zero);
        }

        private Expr PickFullValue(long value, Expr fallback)
        {
            if (fullNonLiteralCache.TryGetValue(value, out var expr))
            {
                return expr;
            }

            if (fullCache.TryGetValue(value, out expr) && !IsLiteral(expr))
            {
                return expr;
            }

            return fallback;
        }

        private static bool NeedParentheses(string parentOp, Expr child, bool isRight)
        {
            if (parentOp == "+")
            {
                if (child.Precedence < PrecAdd)
                {
                    return true;
                }
                if (isRight && child.Kind == "neg")
                {
                    return true;
                }
                return false;
            }

            if (parentOp == "-")
            {
                if (child.Precedence < PrecAdd)
                {
                    return true;
                }
                if (isRight && child.Precedence <= PrecAdd)
                {
                    return true;
                }
                return false;
            }

            if (parentOp == "*")
            {
                if (child.Precedence < PrecMul)
                {
                    return true;
                }
                if (isRight && child.Kind == "/")
                {
                    return true;
                }
                return false;
            }

            if (parentOp == "/")
            {
                if (child.Precedence < PrecMul)
                {
                    return true;
                }
                if (isRight && child.Precedence <= PrecMul)
                {
                    return true;
                }
                return false;
            }

            return false;
        }

        private static string FormatChild(string parentOp, Expr child, bool isRight)
        {
            return NeedParentheses(parentOp, child, isRight) ? $"({child.Text})" : child.Text;
        }

        private static Expr Neg(Expr expr)
        {
            if (IsLiteral(expr))
            {
                return new Expr($"-{expr.Text}", PrecUnary, "neg");
            }
            return new Expr($"-({expr.Text})", PrecUnary, "neg");
        }

        private static Expr Binary(string op, Expr left, Expr right)
        {
            var precedence = (op == "+" || op == "-") ? PrecAdd : PrecMul;
            var leftText = FormatChild(op, left, isRight: false);
            var rightText = FormatChild(op, right, isRight: true);
            return new Expr($"{leftText}{op}{rightText}", precedence, op);
        }

        private (Dictionary<long, Expr> fullTable, Dictionary<long, Expr> fullNonLiteralTable) BuildSplitTable()
        {
            var size = baseDigits.Length;
            var dp = new Dictionary<long, Expr>[size, size + 1];
            for (var i = 0; i < size; i++)
            {
                for (var j = 0; j <= size; j++)
                {
                    dp[i, j] = new Dictionary<long, Expr>();
                }
            }

            for (var length = 1; length <= size; length++)
            {
                for (var start = 0; start <= size - length; start++)
                {
                    var end = start + length;
                    var current = new Dictionary<long, Expr>();
                    var literalText = baseDigits.Substring(start, length);
                    var literalValue = long.Parse(literalText);
                    RememberSeed(current, literalValue, Lit(literalText));

                    for (var split = start + 1; split < end; split++)
                    {
                        var left = dp[start, split];
                        var right = dp[split, end];

                        foreach (var leftPair in left)
                        {
                            foreach (var rightPair in right)
                            {
                                RememberSeed(current, leftPair.Key + rightPair.Key, Binary("+", leftPair.Value, rightPair.Value));
                                RememberSeed(current, leftPair.Key - rightPair.Key, Binary("-", leftPair.Value, rightPair.Value));
                                RememberSeed(current, leftPair.Key * rightPair.Key, Binary("*", leftPair.Value, rightPair.Value));

                                if (rightPair.Key != 0L && leftPair.Key % rightPair.Key == 0L)
                                {
                                    RememberSeed(current, leftPair.Key / rightPair.Key, Binary("/", leftPair.Value, rightPair.Value));
                                }
                            }
                        }
                    }

                    var snapshot = new List<KeyValuePair<long, Expr>>(current);
                    foreach (var pair in snapshot)
                    {
                        if (pair.Key != 0L)
                        {
                            RememberSeed(current, -pair.Key, Neg(pair.Value));
                        }
                    }

                    dp[start, end] = current;
                }
            }

            var fullTable = new Dictionary<long, Expr>(dp[0, size]);
            var fullNonLiteralTable = new Dictionary<long, Expr>();
            foreach (var pair in fullTable)
            {
                if (!IsLiteral(pair.Value))
                {
                    Remember(fullNonLiteralTable, pair.Key, pair.Value);
                }
            }

            return (fullTable, fullNonLiteralTable);
        }

        private (long left, long right)? BestSplit(long num)
        {
            var low = 0;
            var high = seedValues.Count - 1;
            var answer = -1;
            while (low <= high)
            {
                var mid = low + (high - low) / 2;
                if (seedValues[mid] < num)
                {
                    answer = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            if (answer < 0)
            {
                return null;
            }

            var left = seedValues[answer];
            return (left, num - left);
        }

        private void StoreGenerated(long num, Expr expr)
        {
            Remember(cache, num, expr);
            if (!IsLiteral(expr))
            {
                Remember(nonLiteralCache, num, expr);
            }
        }

        private Expr GetExpr(long num)
        {
            if (cache.TryGetValue(num, out var cached))
            {
                return cached;
            }

            if (num != baseValue && fullNonLiteralCache.TryGetValue(num, out var fullExpr))
            {
                StoreGenerated(num, fullExpr);
                return cache[num];
            }

            if (num < 0L)
            {
                var expr = Binary("*", negOneExpr, GetExpr(-num));
                StoreGenerated(num, expr);
                return cache[num];
            }

            if (num > baseValue)
            {
                var div = num / baseValue;
                var mod = num % baseValue;
                var expr = Binary("*", GetExpr(div), baseExpr);
                if (mod != 0L)
                {
                    expr = Binary("+", expr, GetExpr(mod));
                }
                StoreGenerated(num, expr);
                return cache[num];
            }

            var split = BestSplit(num);
            if (split.HasValue)
            {
                var expr = Binary("+", GetExpr(split.Value.left), GetExpr(split.Value.right));
                StoreGenerated(num, expr);
                return cache[num];
            }

            var divByTwo = num / 2L;
            var modByTwo = num % 2L;
            var fallbackExpr = Binary("*", GetExpr(divByTwo), twoExpr);
            if (modByTwo != 0L)
            {
                fallbackExpr = Binary("+", fallbackExpr, oneExpr);
            }
            StoreGenerated(num, fallbackExpr);
            return cache[num];
        }

        private Expr FinalizeExpr(long num, Expr expr)
        {
            if (num == -1L)
            {
                return negOneExpr;
            }
            if (num == 0L)
            {
                return zeroExpr;
            }
            if (num == 1L)
            {
                return oneExpr;
            }
            if (num == 2L)
            {
                return twoExpr;
            }

            if (num != baseValue && fullNonLiteralCache.TryGetValue(num, out var fullExpr))
            {
                return fullExpr;
            }

            Expr result;
            if (num == baseValue || !IsLiteral(expr))
            {
                result = expr;
            }
            else if (nonLiteralCache.TryGetValue(num, out var alt))
            {
                result = alt;
            }
            else if (num > 1L)
            {
                result = Binary("+", GetExpr(num - 1L), oneExpr);
            }
            else
            {
                result = Binary("*", negOneExpr, GetExpr(-num));
            }

            return result;
        }
    }
}
