using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocks_.Core.Services
{
    public class ExpressionEvaluator
    {
        private readonly Dictionary<string, double> variables;
        private readonly HashSet<string> declaredVariables;

        public ExpressionEvaluator(Dictionary<string, double> vars, HashSet<string> declaredVars)
        {
            variables = vars;
            declaredVariables = declaredVars;
        }


        // НОВЫЙ МЕТОД: Проверка и получение значения переменной
        private double GetVariableValue(string name)
        {
            // 1. Проверка объявления переменной
            if (!declaredVariables.Contains(name))
            {
                throw new InvalidOperationException($"Переменная '{name}' не объявлена в блоке 'Описание переменных'.");
            }

            // 2. Получение значения (так как переменная объявлена, она должна быть в словаре, 
            // так как в InitializeVariables мы присваиваем 0, если нет начального значения)
            if (variables.TryGetValue(name, out double value))
            {
                return value;
            }

            // Если дошли сюда, это неожиданный случай (переменная объявлена, но нет в variables),
            // что также можно считать ошибкой неинициализированного использования.
            throw new InvalidOperationException($"Переменная '{name}' объявлена, но не инициализирована (не присвоено начальное значение).");
        }
        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            var tokens = Tokenize(expression);
            var rpn = ToRpn(tokens);
            return EvaluateRpn(rpn);
        }

        private List<string> Tokenize(string expr)
        {
            var tokens = new List<string>();
            var sb = new StringBuilder();

            for (int i = 0; i < expr.Length; i++)
            {
                char c = expr[i];
                if (char.IsWhiteSpace(c))
                    continue;

                if (char.IsLetterOrDigit(c) || c == '.' || c == '_')
                {
                    sb.Clear();
                    sb.Append(c);

                    while (i + 1 < expr.Length &&
                           (char.IsLetterOrDigit(expr[i + 1]) || expr[i + 1] == '.' || expr[i + 1] == '_'))
                    {
                        sb.Append(expr[++i]);
                    }

                    tokens.Add(sb.ToString());
                }
                else
                {
                    // двухсимвольные операторы
                    if (i + 1 < expr.Length)
                    {
                        string two = $"{c}{expr[i + 1]}";
                        if (new[] { ">=", "<=", "==", "!=", "&&", "||" }.Contains(two))
                        {
                            tokens.Add(two);
                            i++;
                            continue;
                        }
                    }
                    tokens.Add(c.ToString());
                }
            }
            return tokens;
        }

        private static int Precedence(string op)
        {
            return op switch
            {
                "||" or "or" => 1,
                "&&" or "and" => 2,
                "==" or "!=" => 3,
                ">" or "<" or ">=" or "<=" => 4,
                "+" or "-" => 5,
                "*" or "/" or "%" => 6,
                _ => 0
            };
        }

        private static bool IsOperator(string token)
        {
            return new[] {
            "+","-","*","/","%","==","!=",
            ">", "<", ">=", "<=",
            "&&", "||","and","or"
        }.Contains(token);
        }

        private List<string> ToRpn(List<string> tokens)
        {
            var output = new List<string>();
            var ops = new Stack<string>();

            foreach (var token in tokens)
            {
                if (double.TryParse(token, out _) || IsVariable(token))
                {
                    output.Add(token);
                }
                else if (token == "(")
                {
                    ops.Push(token);
                }
                else if (token == ")")
                {
                    while (ops.Count > 0 && ops.Peek() != "(")
                        output.Add(ops.Pop());
                    if (ops.Count > 0) ops.Pop(); // убрать "("
                }
                else if (IsOperator(token))
                {
                    while (ops.Count > 0 && IsOperator(ops.Peek()) &&
                           Precedence(ops.Peek()) >= Precedence(token))
                    {
                        output.Add(ops.Pop());
                    }
                    ops.Push(token);
                }
            }

            while (ops.Count > 0)
                output.Add(ops.Pop());

            return output;
        }

        private double EvaluateRpn(List<string> rpn)
        {
            var stack = new Stack<double>();

            foreach (var token in rpn)
            {
                if (double.TryParse(token, out double num))
                {
                    stack.Push(num);
                }
                else if (IsVariable(token))
                {
                    if (variables.TryGetValue(token, out double val))
                        stack.Push(val);
                    else
                        stack.Push(0);
                }
                else if (IsOperator(token))
                {
                    double b = stack.Pop();
                    double a = stack.Count > 0 ? stack.Pop() : 0;
                    stack.Push(ApplyOperator(a, b, token));
                }
                else // Считаем токен именем переменной
                {
                    stack.Push(GetVariableValue(token));
                }
            }

            return stack.Pop();
        }

        private static double ApplyOperator(double a, double b, string op)
        {
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b != 0 ? a / b : 0,
                "%" => a % b,
                ">" => a > b ? 1 : 0,
                "<" => a < b ? 1 : 0,
                ">=" => a >= b ? 1 : 0,
                "<=" => a <= b ? 1 : 0,
                "==" => Math.Abs(a - b) < 0.00001 ? 1 : 0,
                "!=" => Math.Abs(a - b) > 0.00001 ? 1 : 0,
                "&&" or "and" => a != 0 && b != 0 ? 1 : 0,
                "||" or "or" => a != 0 || b != 0 ? 1 : 0,
                _ => 0
            };
        }

        private bool IsVariable(string token)
        {
            if (double.TryParse(token, out _)) return false;
            if (token.All(c => char.IsLetter(c) || c == '_')) return true;
            return variables.ContainsKey(token);
        }
    }

}
