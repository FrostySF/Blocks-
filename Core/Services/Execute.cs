using Blocks_.Core.Models;
using Blocks_.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Blocks_
{
    public class TraceEntry
    {
        public int Step { get; set; }
        public string BlockType { get; set; }
        public string BlockCode { get; set; }
        public string Variable { get; set; }
        public double OldValue { get; set; }
        public double NewValue { get; set; }
        public string Comment { get; set; }
    }
    public partial class MainWindow
    {
        private Dictionary<string, double> variables = new();
        private HashSet<string> declaredVariables = new();
        private Dictionary<string, double> initialVariables = new();
        private List<TraceEntry> traceLog = new();
        // Хранилища для массивов
        private Dictionary<string, double[]> vectors = new();
        private Dictionary<string, double[,]> matrices = new();
        private HashSet<string> declaredArrays = new();

        private Dictionary<BlockItem, bool> forLoopInitialized = new();

        private bool IsIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return (char.IsLetter(name[0]) || name[0] == '_') &&
                   name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }
        private void ClearExecutionState()
        {
            traceLog.Clear();
            initialVariables.Clear();
            forLoopInitialized.Clear();
        }
        private void SaveInitialVariables()
        {
            initialVariables.Clear();
            foreach (var kvp in variables)
            {
                initialVariables.Add(kvp.Key, kvp.Value);
            }
        }
        public void LogVariableChange(BlockItem block, string varName, double oldValue, double newValue, string comment = "")
        {
            if (Math.Abs(oldValue - newValue) > double.Epsilon || traceLog.Count == 0 || !initialVariables.ContainsKey(varName))
            {
                traceLog.Add(new TraceEntry
                {
                    Step = traceLog.Count + 1,
                    BlockType = block.Type.ToString(),
                    BlockCode = block.Code,
                    Variable = varName,
                    OldValue = oldValue,
                    NewValue = newValue,
                    Comment = comment
                });
            }
        }
        private bool InitializeVariables()
        {

            ClearExecutionState();
            ClearVariablePreviewPanels();

            variables.Clear();
            declaredVariables.Clear();
            vectors.Clear();
            matrices.Clear();
            declaredArrays.Clear();

            // Инициализация переменных
            var declarationBlock = listofblocks.FirstOrDefault(b => b.Type == BlockType.VariableDeclaration);
            if (declarationBlock != null && !string.IsNullOrWhiteSpace(declarationBlock.Code))
            {
                if (!InitializeSimpleVariables(declarationBlock.Code))
                    return false;
            }

            // Инициализация массивов
            var arrayBlocks = listofblocks.Where(b => b.Type == BlockType.ArrayDeclaration).ToList();
            foreach (var arrayBlock in arrayBlocks)
            {
                if (!string.IsNullOrWhiteSpace(arrayBlock.Code))
                {
                    if (!InitializeArray(arrayBlock.Code))
                        return false;
                }
            }
            SaveInitialVariables();
            return true;
        }

        private bool InitializeSimpleVariables(string code)
        {
            var evaluator = new ExpressionEvaluator(variables, declaredVariables);
            var lines = code.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var cleanLine = line.Trim().Replace(";", "");
                if (string.IsNullOrWhiteSpace(cleanLine) || cleanLine.StartsWith("//"))
                    continue;

                if (cleanLine.Contains("="))
                {
                    var parts = cleanLine.Split('=', 2).Select(p => p.Trim()).ToArray();
                    var varName = parts[0];
                    var expr = parts[1];

                    if (!IsIdentifier(varName))
                    {
                        TraceTextBlock.Text += $"\n[Error] Некорректное имя переменной: {varName}";
                        return false;
                    }

                    try
                    {
                        double val = evaluator.Evaluate(expr);
                        variables[varName] = val;
                        declaredVariables.Add(varName);
                        TraceTextBlock.Text += $"\n[Declare] {varName} = {val}";
                    }
                    catch (Exception ex)
                    {
                        TraceTextBlock.Text += $"\n[Error] Ошибка инициализации {varName}: {ex.Message}";
                        return false;
                    }
                }
                else
                {
                    var varName = cleanLine;
                    if (!IsIdentifier(varName))
                    {
                        TraceTextBlock.Text += $"\n[Error] Некорректное имя: {varName}";
                        return false;
                    }

                    variables[varName] = 0.0;
                    declaredVariables.Add(varName);
                    TraceTextBlock.Text += $"\n[Declare] {varName}";
                }
            }
            return true;
        }

        private bool InitializeArray(string code)
        {
            try
            {
                var bracketPattern = @"(\w+)\[(\d*)\](\[(\d*)\])?(\s*=\s*\{(.+)\})?";
                var match = Regex.Match(code, bracketPattern);

                if (!match.Success)
                {
                    TraceTextBlock.Text += $"\n[Error] Неверный формат массива: {code}";
                    return false;
                }

                string arrayName = match.Groups[1].Value;
                string size1 = match.Groups[2].Value;
                string size2 = match.Groups[4].Value;
                string valuesStr = match.Groups[6].Value;

                if (!IsIdentifier(arrayName))
                {
                    TraceTextBlock.Text += $"\n[Error] Некорректное имя массива: {arrayName}";
                    return false;
                }

                declaredArrays.Add(arrayName);


                if (!string.IsNullOrEmpty(size2))
                {
                    // Это матрица
                    int rows = string.IsNullOrEmpty(size1) ? 0 : int.Parse(size1);
                    int cols = string.IsNullOrEmpty(size2) ? 0 : int.Parse(size2);

                    if (string.IsNullOrEmpty(valuesStr))
                    {
                        if (rows > 0 && cols > 0)
                        {
                            matrices[arrayName] = new double[rows, cols];
                            TraceTextBlock.Text += $"\n[Array] Матрица {arrayName}[{rows}][{cols}] инициализирована нулями";
                        }
                        else
                        {
                            TraceTextBlock.Text += $"\n[Error] Размеры матрицы должны быть указаны";
                            declaredArrays.Remove(arrayName);
                            return false;
                        }
                    }
                    else
                    {
                        var matrix = ParseMatrixValues(valuesStr, rows, cols);
                        if (matrix == null)
                        {
                            TraceTextBlock.Text += $"\n[Error] Ошибка парсинга значений матрицы";
                            declaredArrays.Remove(arrayName); 
                            return false;
                        }
                        matrices[arrayName] = matrix;
                        TraceTextBlock.Text += $"\n[Array] Матрица {arrayName}[{rows}][{cols}] инициализирована";
                    }
                }
                else
                {
                    // Это вектор
                    int size = string.IsNullOrEmpty(size1) ? 0 : int.Parse(size1);

                    if (string.IsNullOrEmpty(valuesStr))
                    {
                        if (size > 0)
                        {
                            vectors[arrayName] = new double[size];
                            TraceTextBlock.Text += $"\n[Array] Вектор {arrayName}[{size}] инициализирован нулями";
                        }
                        else
                        {
                            TraceTextBlock.Text += $"\n[Error] Размер вектора должен быть указан";
                            declaredArrays.Remove(arrayName); 
                            return false;
                        }
                    }
                    else
                    {
                        var vector = ParseVectorValues(valuesStr);
                        if (vector == null)
                        {
                            TraceTextBlock.Text += $"\n[Error] Ошибка парсинга значений вектора";
                            declaredArrays.Remove(arrayName); 
                            return false;
                        }

                        if (size > 0 && vector.Length != size)
                        {
                            TraceTextBlock.Text += $"\n[Warn] Размер не совпадает. Использован размер из значений: {vector.Length}";
                        }

                        vectors[arrayName] = vector;
                        TraceTextBlock.Text += $"\n[Array] Вектор {arrayName}[{vector.Length}] инициализирован";
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                TraceTextBlock.Text += $"\n[Error] Ошибка при инициализации массива: {ex.Message}";
                return false;
            }
        }

        private double[] ParseVectorValues(string valuesStr)
        {
            try
            {
                var values = valuesStr.Split(',')
                    .Select(v => double.Parse(v.Trim()))
                    .ToArray();
                return values;
            }
            catch
            {
                return null;
            }
        }

        private double[,] ParseMatrixValues(string valuesStr, int rows, int cols)
        {
            try
            {
                var rowStrings = valuesStr.Split(new[] { "},{" }, StringSplitOptions.None);

                if (rows > 0 && rowStrings.Length != rows)
                    return null;

                int actualRows = rowStrings.Length;
                var firstRow = rowStrings[0].Trim('{', '}').Split(',');
                int actualCols = firstRow.Length;

                if (cols > 0 && actualCols != cols)
                    return null;

                var matrix = new double[actualRows, actualCols];

                for (int i = 0; i < actualRows; i++)
                {
                    var rowStr = rowStrings[i].Trim('{', '}');
                    var values = rowStr.Split(',').Select(v => double.Parse(v.Trim())).ToArray();

                    if (values.Length != actualCols)
                        return null;

                    for (int j = 0; j < actualCols; j++)
                    {
                        matrix[i, j] = values[j];
                    }
                }

                return matrix;
            }
            catch
            {
                return null;
            }
        }

        private bool ExecuteBlock(Tree node)
        {
            if (node.Block == null)
                return true;

            if (node.Block.Type == BlockType.VariableDeclaration ||
                node.Block.Type == BlockType.ArrayDeclaration)
            {
                TraceTextBlock.Text += $"\n[{node.Block.Type}] Блок пропущен (инициализация) => {node.Block.Code}";
                return true;
            }

            string code = node.Block.Code?.Trim(',') ?? "";

            var eval = new ArrayExpressionEvaluator(variables, declaredVariables,
                                                         vectors, matrices, declaredArrays);

            try
            {
                switch (node.Block.Type)
                {
                    case BlockType.Decision:
                    case BlockType.While:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        double result = eval.Evaluate(code);
                        TraceTextBlock.Text += $"\n[{node.Block.Type}] {code} → {(result != 0 ? "true" : "false")}";
                        return result != 0;

                    case BlockType.Process:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        if (code.Contains("="))
                        {
                            ExecuteAssignment(code, eval);
                        }
                        else
                        {
                            double val = eval.Evaluate(code);
                            TraceTextBlock.Text += $"\n{code} → {val}";
                        }
                        break;

                    case BlockType.Output:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        ExecuteOutput(code, eval);
                        break;

                    case BlockType.Input:
                        if (string.IsNullOrWhiteSpace(code))
                        {
                            TraceTextBlock.Text += $"\n[Error] Блок ввода пустой";
                            return false;
                        }
                        if (declaredArrays.Contains(code))
                        {
                            var task = InputArrayShow(code);
                        }
                        else if (declaredVariables.Contains(code))
                        {
                            var task = InputShow(node.Block);
                        }
                        else
                        {
                            TraceTextBlock.Text += $"\n[Error] '{code}' не объявлена";
                            return false;
                        }
                        break;

                    case BlockType.DoWhile:
                        TraceTextBlock.Text += $"\n[DO-WHILE] Вход в тело цикла !!!";
                        return true; 
                    case BlockType.For:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        var forParts = code.Split(';').Select(p => p.Trim()).ToArray();

                        if (forParts.Length != 3)
                        {
                            TraceTextBlock.Text += $"\n[Error] Неверный формат FOR: {code}. ";
                            return false;
                        }

                        string init = forParts[0];
                        string condition = forParts[1];
                        if (!forLoopInitialized.ContainsKey(node.Block) || !forLoopInitialized[node.Block])
                        {
                            ExecuteAssignment(init, eval);
                            forLoopInitialized[node.Block] = true;
                            ShowNotification("for loop");
                            TraceTextBlock.Text += $"\n[FOR] Инициализация: {init}";
                        }

                        double condResult = eval.Evaluate(condition);
                        TraceTextBlock.Text += $"\n[FOR] Условие: {condition} → {(condResult != 0 ? "true" : "false")}";

                        if (condResult != 0)
                        {
                            return true;
                        }
                        else
                        {
                            forLoopInitialized[node.Block] = false;
                            return false;
                        }

                    case BlockType.LoopConnector:

                        BlockItem loopBlock = FindParentLoop(node.Block);

                        if (loopBlock != null)
                        {
                            if (loopBlock.Type == BlockType.For)
                            {
                                var forParts2 = loopBlock.Code.Split(';').Select(p => p.Trim()).ToArray();
                                if (forParts2.Length == 3)
                                {
                                    string step = forParts2[2];
                                    ExecuteAssignment(step, eval);
                                    TraceTextBlock.Text += $"\n[FOR] Инкремент: {step}";
                                }
                            }
                            else if (loopBlock.Type == BlockType.While)
                            {
                                TraceTextBlock.Text += $"\n[WHILE] Возврат к проверке условия";
                            }
                        }
                        return true;

                    case BlockType.DoLoopConnector:
                        var doParentConnection = connectionLines.FirstOrDefault(cl => cl.ToBlock == node.Block);

                        if (doParentConnection != null && doParentConnection.FromBlock.Type == BlockType.DoWhile)
                        {
                            var doWhileBlock = doParentConnection.FromBlock;
                            string doCondition = doWhileBlock.Code?.Trim() ?? "";

                            if (!string.IsNullOrWhiteSpace(doCondition))
                            {
                                double doResult = eval.Evaluate(doCondition);
                                TraceTextBlock.Text += $"\n[DO-WHILE] Условие: {doCondition} → {(doResult != 0 ? "true (повтор)" : "false (выход)")}";
                                return doResult != 0; 
                            }
                        }

                        return true;
                }

                UpdateBlockVariableState(node.Block, variables);
                return true;
            }
            catch (Exception ex)
            {
                TraceTextBlock.Text += $"\nОшибка: {ex.Message}";
                return false;
            }
        }
        private BlockItem FindParentLoop(BlockItem connectorBlock)
        {
            var visited = new HashSet<BlockItem>();
            var current = connectorBlock;

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);

                var incomingConnection = connectionLines.FirstOrDefault(cl => cl.ToBlock == current);

                if (incomingConnection == null)
                    break;

                var fromBlock = incomingConnection.FromBlock;
                if (fromBlock.Type == BlockType.For ||
                    fromBlock.Type == BlockType.While ||
                    fromBlock.Type == BlockType.DoWhile)
                    return fromBlock;

                current = fromBlock;
            }

            return null;
        }
        private void ExecuteOutput(string code, ArrayExpressionEvaluator evaluator)
        {
            if (declaredArrays.Contains(code))
            {
                if (vectors.TryGetValue(code, out var vector))
                {
                    var values = string.Join(", ", vector.Select(v => v.ToString("G5")));
                    TraceTextBlock.Text += $"\n[Output] {code}[]: [{values}]";
                }
                else if (matrices.TryGetValue(code, out var matrix))
                {
                    var sb = new StringBuilder();
                    sb.Append($"\n[Output] {code}[][]:");
                    for (int i = 0; i < matrix.GetLength(0); i++)
                    {
                        sb.Append("\n  [");
                        for (int j = 0; j < matrix.GetLength(1); j++)
                        {
                            sb.Append(matrix[i, j].ToString("G5"));
                            if (j < matrix.GetLength(1) - 1)
                                sb.Append(", ");
                        }
                        sb.Append("]");
                    }
                    TraceTextBlock.Text += sb.ToString();
                }
            }
            else
            {
                // Вывод выражения или переменной
                double outVal = evaluator.Evaluate(code);
                TraceTextBlock.Text += $"\n[Output]: {outVal}";
            }
        }

        private void ExecuteAssignment(string code, ArrayExpressionEvaluator evaluator)
        {
            var parts = code.Split('=', 2);
            var leftSide = parts[0].Trim();
            var rightSide = parts[1].Trim();

            var indexPattern = @"(\w+)\[(.+?)\](\[(.+?)\])?";
            var match = Regex.Match(leftSide, indexPattern);

            if (match.Success)
            {
                string arrayName = match.Groups[1].Value;
                string index1Expr = match.Groups[2].Value;
                string index2Expr = match.Groups[4].Value;

                if (!declaredArrays.Contains(arrayName))
                {
                    throw new InvalidOperationException($"Массив '{arrayName}' не объявлен");
                }

                int index1 = (int)evaluator.Evaluate(index1Expr);
                double value = evaluator.Evaluate(rightSide);

                if (!string.IsNullOrEmpty(index2Expr))
                {
                    int index2 = (int)evaluator.Evaluate(index2Expr);
                    if (matrices.TryGetValue(arrayName, out var matrix))
                    {
                        if (index1 >= 0 && index1 < matrix.GetLength(0) &&
                            index2 >= 0 && index2 < matrix.GetLength(1))
                        {
                            matrix[index1, index2] = value;
                            TraceTextBlock.Text += $"\n{arrayName}[{index1}][{index2}] = {value}";
                        }
                        else
                        {
                            throw new IndexOutOfRangeException($"Индекс вне диапазона: {arrayName}[{index1}][{index2}]");
                        }
                    }
                }
                else
                {
                    if (vectors.TryGetValue(arrayName, out var vector))
                    {
                        if (index1 >= 0 && index1 < vector.Length)
                        {
                            vector[index1] = value;
                            TraceTextBlock.Text += $"\n{arrayName}[{index1}] = {value}";
                        }
                        else
                        {
                            throw new IndexOutOfRangeException($"Индекс вне диапазона: {arrayName}[{index1}]");
                        }
                    }
                }
            }
            else
            {
                var varName = leftSide;

                if (!declaredVariables.Contains(varName))
                {
                    throw new InvalidOperationException($"Переменная '{varName}' не объявлена");
                }

                double val = evaluator.Evaluate(rightSide);
                variables[varName] = val;
                TraceTextBlock.Text += $"\n{varName} = {val}";
            }
        }

        private async Task InputArrayShow(string arrayName)
        {
            TraceTextBlock.Text += $"\n[Input Array] Запрос ввода массива: {arrayName}";

            // Определяем тип массива
            bool isVector = vectors.ContainsKey(arrayName);
            bool isMatrix = matrices.ContainsKey(arrayName);

            if (!isVector && !isMatrix)
            {
                TraceTextBlock.Text += $"\n[Error] Массив {arrayName} не найден";
                return;
            }

            var mainPanel = new StackPanel { Spacing = 10 };

            if (isVector)
            {
                var vector = vectors[arrayName];
                var titleText = new TextBlock
                {
                    Text = $"Ввод значений для вектора '{arrayName}' (размер: {vector.Length})",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                mainPanel.Children.Add(titleText);

                var inputBoxes = new List<TextBox>();

                for (int i = 0; i < vector.Length; i++)
                {
                    var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };

                    var label = new TextBlock
                    {
                        Text = $"{arrayName}[{i}]:",
                        Width = 80,
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    var inputBox = new TextBox
                    {
                        Text = vector[i].ToString(),
                        PlaceholderText = "0",
                        Width = 150
                    };

                    inputBox.KeyDown += (sender, e) =>
                    {
                        if (e.Key == Windows.System.VirtualKey.Enter)
                        {
                            e.Handled = true;

                            var currentTextBox = (TextBox)sender;

                            var parentPanel = (StackPanel)currentTextBox.Parent; 
                            var grandParentPanel = (StackPanel)parentPanel.Parent; 

                            int currentIndex = grandParentPanel.Children.IndexOf(parentPanel);
                            int nextIndex = currentIndex + 1;

                            if (nextIndex < grandParentPanel.Children.Count)
                            {
                                var nextRowPanel = grandParentPanel.Children[nextIndex] as StackPanel;

                                var nextTextBox = nextRowPanel?.Children[1] as TextBox;

                                if (nextTextBox != null)
                                {
                                    nextTextBox.Focus(FocusState.Keyboard);
                                    nextTextBox.SelectAll();
                                    return;
                                }
                            }
                        }
                    };

                    inputBoxes.Add(inputBox);
                    rowPanel.Children.Add(label);
                    rowPanel.Children.Add(inputBox);
                    mainPanel.Children.Add(rowPanel);
                }

                var scrollViewer = new ScrollViewer
                {
                    Content = mainPanel,
                    MaxHeight = 400
                };

                var dialog = new ContentDialog
                {
                    Title = $"Ввод вектора: {arrayName}",
                    Content = scrollViewer,
                    PrimaryButtonText = "Сохранить",
                    CloseButtonText = "Отмена",
                    XamlRoot = Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    for (int i = 0; i < vector.Length; i++)
                    {
                        if (double.TryParse(inputBoxes[i].Text, out double value))
                        {
                            vector[i] = value;
                        }
                        else
                        {
                            vector[i] = 0;
                        }
                    }

                    var values = string.Join(", ", vector.Select(v => v.ToString("G5")));
                    TraceTextBlock.Text += $"\n[Input] {arrayName}[] = [{values}]";
                }
            }
            else if (isMatrix)
            {
                var matrix = matrices[arrayName];
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                var titleText = new TextBlock
                {
                    Text = $"Ввод значений для матрицы '{arrayName}' ({rows}x{cols})",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                mainPanel.Children.Add(titleText);

                var inputBoxes = new TextBox[rows, cols];

                for (int i = 0; i < rows; i++)
                {
                    var rowPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5 };

                    var rowLabel = new TextBlock
                    {
                        Text = $"Строка {i}:",
                        Width = 70,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    rowPanel.Children.Add(rowLabel);

                    for (int j = 0; j < cols; j++)
                    {
                        var inputBox = new TextBox
                        {
                            Text = matrix[i, j].ToString(),
                            PlaceholderText = "0",
                            Width = 60,
                            Margin = new Thickness(2)
                        };
                        inputBox.KeyDown += (sender, e) =>
                        {
                            if (e.Key == Windows.System.VirtualKey.Enter)
                            {
                                e.Handled = true;


                                int currentI = -1, currentJ = -1;
                                var currentTextBox = (TextBox)sender;
                                for (int findI = 0; findI < rows; findI++)
                                {
                                    for (int findJ = 0; findJ < cols; findJ++)
                                    {
                                        if (inputBoxes[findI, findJ] == currentTextBox)
                                        {
                                            currentI = findI;
                                            currentJ = findJ;
                                            break;
                                        }
                                    }
                                    if (currentI != -1) break;
                                }

                                if (currentI == -1) return;

                                int nextI = currentI;
                                int nextJ = currentJ + 1;

                                if (nextJ >= cols)
                                {
                                    nextI = currentI + 1;
                                    nextJ = 0;
                                }
                                if (nextI < rows)
                                {
                                    var nextTextBox = inputBoxes[nextI, nextJ];
                                    nextTextBox.Focus(FocusState.Keyboard);
                                    nextTextBox.SelectAll();
                                }
                                else
                                {
                                    
                                }
                            }
                        };

                        inputBoxes[i, j] = inputBox;
                        rowPanel.Children.Add(inputBox);
                    }

                    mainPanel.Children.Add(rowPanel);
                }

                var scrollViewer = new ScrollViewer
                {
                    Content = mainPanel,
                    MaxHeight = 450
                };

                var dialog = new ContentDialog
                {
                    Title = $"Ввод матрицы: {arrayName}",
                    Content = scrollViewer,
                    PrimaryButtonText = "Сохранить",
                    CloseButtonText = "Отмена",
                    XamlRoot = Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < cols; j++)
                        {
                            if (double.TryParse(inputBoxes[i, j].Text, out double value))
                            {
                                matrix[i, j] = value;
                            }
                            else
                            {
                                matrix[i, j] = 0;
                            }
                        }
                    }

                    var sb = new StringBuilder();
                    sb.Append($"\n[Input] {arrayName}[][] =");
                    for (int i = 0; i < rows; i++)
                    {
                        sb.Append("\n  [");
                        for (int j = 0; j < cols; j++)
                        {
                            sb.Append(matrix[i, j].ToString("G5"));
                            if (j < cols - 1)
                                sb.Append(", ");
                        }
                        sb.Append("]");
                    }
                    TraceTextBlock.Text += sb.ToString();
                }
            }
        }
        #region TRACe!
        public async void ShowTraceResults()
        {
            var stackPanel = new StackPanel { Spacing = 10, Padding = new Thickness(10) };

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Начальные и Конечные значения",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 0, 0, 10)
            });

            var varGrid = new Grid();
            varGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            varGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            varGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var headerStyle = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray);
            var borderThickness = new Thickness(0.5);

            // Заголовки таблицы переменных
            AddCell(varGrid, 0, 0, "Переменная", true, headerStyle, borderThickness);
            AddCell(varGrid, 0, 1, "Начальное значение", true, headerStyle, borderThickness);
            AddCell(varGrid, 0, 2, "Конечное значение", true, headerStyle, borderThickness);

            var allVariables = variables.Keys.Union(initialVariables.Keys).Distinct().ToList();
            int row = 1;

            foreach (var varName in allVariables)
            {
                string initial = initialVariables.ContainsKey(varName) ? initialVariables[varName].ToString("0.###") : "—";
                string final = variables.ContainsKey(varName) ? variables[varName].ToString("0.###") : "—";

                AddCell(varGrid, row, 0, varName, false, null, borderThickness);
                AddCell(varGrid, row, 1, initial, false, null, borderThickness);
                AddCell(varGrid, row, 2, final, false, null, borderThickness);
                row++;
            }

            var varScrollViewer = new ScrollViewer
            {
                Content = varGrid,
                MaxHeight = 250,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            stackPanel.Children.Add(varScrollViewer);
            stackPanel.Children.Add(new Microsoft.UI.Xaml.Shapes.Rectangle { Height = 1, Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Gray), Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 10) });

            stackPanel.Children.Add(new TextBlock
            {
                Text = "Таблица трассировки",
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Margin = new Microsoft.UI.Xaml.Thickness(0, 10, 0, 10)
            });

            var traceGrid = new Grid();
            traceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // №
            traceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Блок/Код
            traceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // Переменная
            traceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Изменение
            traceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) }); // Комментарий

            row = 0;
            AddCell(traceGrid, row, 0, "№", true, headerStyle, borderThickness);
            AddCell(traceGrid, row, 1, "Блок/Код", true, headerStyle, borderThickness);
            AddCell(traceGrid, row, 2, "Переменная", true, headerStyle, borderThickness);
            AddCell(traceGrid, row, 3, "Изменение", true, headerStyle, borderThickness);
            AddCell(traceGrid, row, 4, "Комментарий", true, headerStyle, borderThickness);
            row++;

            foreach (var entry in traceLog)
            {
                AddCell(traceGrid, row, 0, entry.Step.ToString(), false, null, borderThickness);
                AddCell(traceGrid, row, 1, $"{entry.BlockType}\n({entry.BlockCode})", false, null, borderThickness);
                AddCell(traceGrid, row, 2, entry.Variable, false, null, borderThickness);
                AddCell(traceGrid, row, 3, $"{entry.OldValue.ToString("0.###")} → {entry.NewValue.ToString("0.###")}", false, null, borderThickness);
                AddCell(traceGrid, row, 4, entry.Comment, false, null, borderThickness);
                row++;
            }

            var traceScrollViewer = new ScrollViewer
            {
                Content = traceGrid,
                MaxHeight = 400,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            //stackPanel.Children.Add(traceScrollViewer);

            var dialog = new ContentDialog
            {
                Title = "Результаты выполнения блок-схемы",
                Content = stackPanel,
                CloseButtonText = "Закрыть",
                SecondaryButtonText = "Копировать данные",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
                CopyTraceReportToClipboard();

        }

        private void AddCell(Grid grid, int row, int column, string text, bool isHeader, Microsoft.UI.Xaml.Media.Brush background, Microsoft.UI.Xaml.Thickness borderThickness)
        {
            if (grid.RowDefinitions.Count <= row)
            {
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            }

            var textBlock = new TextBlock
            {
                Text = text,
                Margin = new Thickness(5),
                FontWeight = isHeader ? Microsoft.UI.Text.FontWeights.SemiBold : Microsoft.UI.Text.FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            var border = new Border
            {
                Child = textBlock,
                BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.LightGray),
                BorderThickness = borderThickness,
                Padding = new Thickness(5),
                Background = background
            };

            Grid.SetRow(border, row);
            Grid.SetColumn(border, column);
            grid.Children.Add(border);
        }
 

        private async Task InputShow(BlockItem block)
        {
            var descBox = new TextBox
            {
                Text = variables.ContainsKey(block.Code) ? variables[block.Code].ToString() : "",
                PlaceholderText = "Введите значение",
                AcceptsReturn = false
            };

            var panel = new StackPanel { Spacing = 10 };
            var label = new TextBlock { Text = $"Введите значение для '{block.Code}':" };
            panel.Children.Add(label);
            panel.Children.Add(descBox);

            var dialog = new ContentDialog
            {
                Title = $"Ввод переменной",
                Content = panel,
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };
            bool isPrimaryResultSimulated = false;

            descBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    e.Handled = true;
                    isPrimaryResultSimulated = true;
                    dialog.Hide();
                }
            };


            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary || isPrimaryResultSimulated)
            {
                if (double.TryParse(descBox.Text, out double value))
                {
                    double oldValue = variables.ContainsKey(block.Code) ? variables[block.Code] : 0.0;
                    variables[block.Code] = value;
                    LogVariableChange(block, block.Code, oldValue, value, "Ввод пользователя");
                    TraceTextBlock.Text += $"\n[Input] {block.Code} = {value}";
                }
                else
                {
                    TraceTextBlock.Text += $"\n[Error] Некорректное значение для {block.Code}";
                }
            }
        }
        #endregion  
    }
}