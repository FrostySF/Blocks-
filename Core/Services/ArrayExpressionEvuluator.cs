using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Blocks_.Core.Services
{
    public class ArrayExpressionEvaluator
    {
        private readonly Dictionary<string, double> variables;
        private readonly HashSet<string> declaredVariables;

        private readonly Dictionary<string, double[]> vectors;
        private readonly Dictionary<string, double[,]> matrices;
        private readonly HashSet<string> declaredArrays;

        public ArrayExpressionEvaluator(
            Dictionary<string, double> vars,
            HashSet<string> declaredVars,
            Dictionary<string, double[]> vecs,
            Dictionary<string, double[,]> mats,
            HashSet<string> declaredArrs)
        {
            variables = vars;
            declaredVariables = declaredVars;
            vectors = vecs;
            matrices = mats;
            declaredArrays = declaredArrs;
        }

        private double GetVariableValue(string name)
        {
            if (!declaredVariables.Contains(name))
            {
                throw new InvalidOperationException($"Переменная '{name}' не объявлена");
            }

            if (variables.TryGetValue(name, out double value))
            {
                return value;
            }

            throw new InvalidOperationException($"Переменная '{name}' не инициализирована");
        }

        public double Evaluate(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            expression = PreprocessArrayAccess(expression);

            var tokens = Tokenize(expression);
            var rpn = ToRpn(tokens);
            return EvaluateRpn(rpn);
        }

        /// <summary>
        ///  arr[i] → temp0, matrix[i][j] → temp1
        /// </summary>
        private string PreprocessArrayAccess(string expr)
        {
            var pattern = @"(\w+)\[([^\[\]]+)\](\[([^\[\]]+)\])?";
            var tempVars = new Dictionary<string, double>();
            int tempCounter = 0;

            expr = Regex.Replace(expr, pattern, match =>
            {
                string arrayName = match.Groups[1].Value;
                string index1Expr = match.Groups[2].Value;
                string index2Expr = match.Groups[4].Value;

                if (!declaredArrays.Contains(arrayName))
                    return match.Value;

                try
                {
                    int index1 = (int)EvaluateSimple(index1Expr);

                    if (!string.IsNullOrEmpty(index2Expr))
                    {
                        int index2 = (int)EvaluateSimple(index2Expr);

                        if (matrices.TryGetValue(arrayName, out var matrix))
                        {
                            if (index1 >= 0 && index1 < matrix.GetLength(0) &&
                                index2 >= 0 && index2 < matrix.GetLength(1))
                            {
                                double value = matrix[index1, index2];
                                string tempVar = $"temp{tempCounter++}";
                                variables[tempVar] = value;
                                declaredVariables.Add(tempVar);
                                return tempVar;
                            }
                            else
                                throw new IndexOutOfRangeException($"Индекс вне диапазона: {arrayName}[{index1}][{index2}]");
                        }
                        else
                            throw new InvalidOperationException($"Матрица '{arrayName}' не найдена");
                    }
                    else
                    {
                        if (vectors.TryGetValue(arrayName, out var vector))
                        {
                            if (index1 >= 0 && index1 < vector.Length)
                            {
                                double value = vector[index1];
                                string tempVar = $"temp{tempCounter++}";
                                variables[tempVar] = value;
                                declaredVariables.Add(tempVar);
                                return tempVar;
                            }
                            else
                            {
                                throw new IndexOutOfRangeException($"Индекс вне диапазона: {arrayName}[{index1}]");
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Вектор '{arrayName}' не найден");
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Ошибка доступа к массиву: {ex.Message}");
                }
            });

            return expr;
        }

        private double EvaluateSimple(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return 0;

            if (double.TryParse(expression.Trim(), out double num))
                return num;

            if (variables.TryGetValue(expression.Trim(), out double val))
                return val;

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
                    if (ops.Count > 0) ops.Pop();
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
                else
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