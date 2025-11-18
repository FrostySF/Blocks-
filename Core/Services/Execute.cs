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

namespace Blocks_
{
    public partial class MainWindow
    {
        private Dictionary<string, double> variables = new();
        private HashSet<string> declaredVariables = new();

        // Хранилища для массивов
        private Dictionary<string, double[]> vectors = new();
        private Dictionary<string, double[,]> matrices = new();
        private HashSet<string> declaredArrays = new();

        private bool IsIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return (char.IsLetter(name[0]) || name[0] == '_') &&
                   name.All(c => char.IsLetterOrDigit(c) || c == '_');
        }

        private bool InitializeVariables()
        {
            ClearVariablePreviewPanels();

            variables.Clear();
            declaredVariables.Clear();
            vectors.Clear();
            matrices.Clear();
            declaredArrays.Clear();

            // Инициализация простых переменных
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

                // ВАЖНО: Сразу добавляем имя массива в declaredArrays перед парсингом значений
                declaredArrays.Add(arrayName);

                // Проверка: матрица или вектор
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
                            declaredArrays.Remove(arrayName); // Откатываем регистрацию
                            return false;
                        }
                    }
                    else
                    {
                        var matrix = ParseMatrixValues(valuesStr, rows, cols);
                        if (matrix == null)
                        {
                            TraceTextBlock.Text += $"\n[Error] Ошибка парсинга значений матрицы";
                            declaredArrays.Remove(arrayName); // Откатываем регистрацию
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
                            declaredArrays.Remove(arrayName); // Откатываем регистрацию
                            return false;
                        }
                    }
                    else
                    {
                        var vector = ParseVectorValues(valuesStr);
                        if (vector == null)
                        {
                            TraceTextBlock.Text += $"\n[Error] Ошибка парсинга значений вектора";
                            declaredArrays.Remove(arrayName); // Откатываем регистрацию
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

            // Пропускаем блоки объявлений
            if (node.Block.Type == BlockType.VariableDeclaration ||
                node.Block.Type == BlockType.ArrayDeclaration)
            {
                TraceTextBlock.Text += $"\n[{node.Block.Type}] Блок пропущен (инициализация)";
                return true;
            }

            string code = node.Block.Code?.Trim() ?? "";

            var evaluator = new ArrayExpressionEvaluator(variables, declaredVariables,
                                                         vectors, matrices, declaredArrays);

            try
            {
                switch (node.Block.Type)
                {
                    case BlockType.Decision:
                    case BlockType.While:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        double result = evaluator.Evaluate(code);
                        TraceTextBlock.Text += $"\n[{node.Block.Type}] {code} → {(result != 0 ? "true" : "false")}";
                        return result != 0;

                    case BlockType.Process:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        if (code.Contains("="))
                        {
                            ExecuteAssignment(code, evaluator);
                        }
                        else
                        {
                            double val = evaluator.Evaluate(code);
                            TraceTextBlock.Text += $"\n{code} → {val}";
                        }
                        break;

                    case BlockType.Output:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        ExecuteOutput(code, evaluator);
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
                            TraceTextBlock.Text += $"\n[Error] '{code}' не объявлена. Проверьте:";
                            TraceTextBlock.Text += $"\n  - Для массивов: создайте блок 'Массивы' с именем '{code}'";
                            TraceTextBlock.Text += $"\n  - Для переменных: добавьте '{code}' в блок 'Описание переменных'";
                            return false;
                        }
                        break;

                    case BlockType.DoWhile:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;
                        double doWhileResult = evaluator.Evaluate(code);
                        TraceTextBlock.Text += $"\n[DO-WHILE] {code} → {(doWhileResult != 0 ? "true (повтор)" : "false (выход)")}";
                        return doWhileResult != 0;

                    case BlockType.For:
                        if (string.IsNullOrWhiteSpace(code))
                            return true;

                        // Разбираем конструкцию FOR: "init; condition; step"
                        var forParts = code.Split(';').Select(p => p.Trim()).ToArray();

                        if (forParts.Length == 3)
                        {
                            string init = forParts[0];
                            string condition = forParts[1];
                            string step = forParts[2];

                            if (!node.Block.Code.Contains("__initialized__"))
                            {
                                ExecuteAssignment(init, evaluator);
                                node.Block.Code += "__initialized__"; // Маркер инициализации
                                TraceTextBlock.Text += $"\n[FOR] Инициализация: {init}";
                            }

                            // Проверяем условие
                            double condResult = evaluator.Evaluate(condition);
                            TraceTextBlock.Text += $"\n[FOR] Условие: {condition} → {(condResult != 0 ? "true" : "false")}";

                            if (condResult != 0)
                            {
                                return true;
                            }
                            else
                            {
                                node.Block.Code = node.Block.Code.Replace("__initialized__", "");
                                return false;
                            }
                        }
                        else
                        {
                            TraceTextBlock.Text += $"\n[Error] Неверный формат FOR: {code}";
                            return false;
                        }


                    case BlockType.LoopConnector:
                        var parentConnection = connectionLines.FirstOrDefault(cl => cl.ToBlock == node.Block);
                        if (parentConnection != null && parentConnection.FromBlock.Type == BlockType.For)
                        {
                            var forBlock = parentConnection.FromBlock;
                            var forParts2 = forBlock.Code.Replace("__initialized__", "").Split(';').Select(p => p.Trim()).ToArray();

                            if (forParts2.Length == 3)
                            {
                                string step = forParts2[2];
                                ExecuteAssignment(step, evaluator);
                                TraceTextBlock.Text += $"\n[FOR] Инкремент: {step}";
                            }
                        }

                        TraceTextBlock.Text += $"\n[Junction] Обратный переход";
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

        private void ExecuteOutput(string code, ArrayExpressionEvaluator evaluator)
        {
            // Проверяем, является ли это массивом
            if (declaredArrays.Contains(code))
            {
                // Вывод всего массива
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
                // Ввод вектора
                var vector = vectors[arrayName];
                var titleText = new TextBlock
                {
                    Text = $"Ввод значений для вектора '{arrayName}' (размер: {vector.Length})",
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold
                };
                mainPanel.Children.Add(titleText);

                var inputBoxes = new List<TextBox>();

                // Создаем поля ввода для каждого элемента
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

                    // --- НОВОЕ: Обработчик нажатия клавиши Enter ---
                    inputBox.KeyDown += (sender, e) =>
                    {
                        if (e.Key == Windows.System.VirtualKey.Enter)
                        {
                            e.Handled = true; // Останавливаем стандартную обработку (если есть)

                            // Находим текущий элемент (текстбокс)
                            var currentTextBox = (TextBox)sender;

                            // Пытаемся найти следующий элемент в родительском StackPanel
                            var parentPanel = (StackPanel)currentTextBox.Parent; // Это rowPanel
                            var grandParentPanel = (StackPanel)parentPanel.Parent; // Это mainPanel

                            int currentIndex = grandParentPanel.Children.IndexOf(parentPanel);

                            // Индекс следующего элемента
                            int nextIndex = currentIndex + 1;

                            if (nextIndex < grandParentPanel.Children.Count)
                            {
                                // Находим следующий StackPanel (rowPanel)
                                var nextRowPanel = grandParentPanel.Children[nextIndex] as StackPanel;

                                // Находим следующий TextBox внутри него (второй элемент в rowPanel)
                                var nextTextBox = nextRowPanel?.Children[1] as TextBox;

                                if (nextTextBox != null)
                                {
                                    // Переводим фокус на следующий TextBox
                                    nextTextBox.Focus(FocusState.Keyboard);
                                    return;
                                }
                            }
                        }
                    };
                    // --------------------------------------------------

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
                // Ввод матрицы
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

                // Создаем таблицу полей ввода
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

                        // --- ОБНОВЛЕННЫЙ: Обработчик нажатия клавиши Enter для матрицы ---
                        inputBox.KeyDown += (sender, e) =>
                        {
                            if (e.Key == Windows.System.VirtualKey.Enter)
                            {
                                e.Handled = true;

                                // Ищем текущие индексы (i, j) в массиве inputBoxes
                                int currentI = -1, currentJ = -1;
                                var currentTextBox = (TextBox)sender;

                                // Необходимо найти индексы текущего TextBox в массиве inputBoxes
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

                                if (currentI == -1) return; // Элемент не найден

                                int nextI = currentI;
                                int nextJ = currentJ + 1; // Сначала пытаемся перейти к следующему столбцу

                                // Проверяем, не вышли ли за пределы строки
                                if (nextJ >= cols)
                                {
                                    nextI = currentI + 1;
                                    nextJ = 0;
                                }
                                if (nextI < rows)
                                {
                                    var nextTextBox = inputBoxes[nextI, nextJ];
                                    nextTextBox.Focus(FocusState.Keyboard);
                                }
                                else
                                {
                                    
                                }
                            }
                        };
                        // --------------------------------------------------

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
            // 1. Создаем флаг, чтобы отследить нажатие Enter
            bool isPrimaryResultSimulated = false;

            descBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    e.Handled = true;
                    isPrimaryResultSimulated = true; // 2. Устанавливаем флаг
                    dialog.Hide(); // 3. Вызываем Hide() без аргументов
                }
            };


            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary || isPrimaryResultSimulated)
            {
                if (double.TryParse(descBox.Text, out double value))
                {
                    variables[block.Code] = value;
                    TraceTextBlock.Text += $"\n[Input] {block.Code} = {value}";
                }
                else
                {
                    TraceTextBlock.Text += $"\n[Error] Некорректное значение для {block.Code}";
                }
            }
        }
    }
}