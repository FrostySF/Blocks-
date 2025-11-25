using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Clipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {

        /// <summary>
        /// Вычисляет расстояние между двумя точками
        /// </summary>
        private double Distance(Point p1, Point p2) => Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

        /// <summary>
        /// Привязка координаты к виртуальной сетке
        /// </summary>
        private double SnapToGrid(double value)
        {
            if (!SettingsWindow.AppSettings.SnapToGrid)
                return value;
            int gridStep = SettingsWindow.AppSettings.GridStep;
            return Math.Round(value / gridStep) * gridStep;
        }

        /// <summary>
        /// Генерирует форматированный текст отчета о трассировке для копирования в буфер обмена.
        /// </summary>
        private string GenerateTraceReportText()
        {
            var sb = new StringBuilder();
            string separator = new string('-', 80);

            sb.AppendLine("Переменная\tНачальное значение\tКонечное значение");
            sb.AppendLine(separator);

            var allVariables = variables.Keys.Union(initialVariables.Keys).Distinct().ToList();

            foreach (var varName in allVariables)
            {
                string initial = initialVariables.ContainsKey(varName) ? initialVariables[varName].ToString("0.###") : "—";
                string final = variables.ContainsKey(varName) ? variables[varName].ToString("0.###") : "—";
                sb.AppendLine($"{varName}\t{initial}\t{final}");
            }

            sb.AppendLine("\n");

            // Таблица трассировки
            sb.AppendLine("ТАБЛИЦА ТРАССИРОВКИ");
            sb.AppendLine("№\tБлок/Код\tПеременная\tСтарое значение\tНовое значение\tКомментарий");
            sb.AppendLine(separator);

            foreach (var entry in traceLog)
            {
                string blockInfo = $"{entry.BlockType} ({entry.BlockCode})";
                string oldVal = entry.OldValue.ToString("0.###");
                string newVal = entry.NewValue.ToString("0.###");

                // Используем символ табуляции для разделения столбцов
                sb.AppendLine($"{entry.Step}\t{blockInfo}\t{entry.Variable}\t{oldVal}\t{newVal}\t{entry.Comment}");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Копирует сгенерированный отчет в буфер обмена.
        /// </summary>
        private void CopyTraceReportToClipboard()
        {
            var reportText = GenerateTraceReportText();
            var dataPackage = new DataPackage();
            dataPackage.SetText(reportText);
            Clipboard.SetContent(dataPackage);
            ShowNotification("Успешно скопированно!");
        }

        public Tree GetSyntaxTree() => syntaxTreeRoot;
        private async void About_Click(object sender, RoutedEventArgs e)
        {
  
            string version = GetAppVersion();

            var dialog = new ContentDialog
            {
                Title = "О программе",
                Content =
                    $"Блок-схема редактор\n" +
                    $"Было сделано для замены 9_14\n" +
                    $"Сделал Хару и делаю лапками!\n" +
                    $"Версия {version}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private string GetAppVersion()
        {
            if (Package.Current != null)
            {
                var v = Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly().GetName().Version;
                return assembly != null
                    ? $"{assembly.Major}.{assembly.Minor}.{assembly.Build}.{assembly.Revision}"
                    : "1.0.0.0";
            }
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://chinoharu.ru/blocks/docs";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowNotification($"Не удалось открыть ссылку: {ex.Message}");
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            currentZoom += 0.1;
            ApplyZoom();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentZoom > 0.2)
                currentZoom -= 0.1;
            ApplyZoom();
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            MainScrollViewer.ChangeView(
                MainScrollViewer.HorizontalOffset,
                MainScrollViewer.VerticalOffset,
                (float)currentZoom);
        }

        private void InitializeBlockContextMenu(Border blockControl)
        {
            var menu = new MenuFlyout();

            var deleteItem = new MenuFlyoutItem
            {
                Text = "Удалить",
                Icon = new FontIcon { Glyph = "\xE74D" }
            };
            deleteItem.Click += (s, e) => DeleteBlock(blockControl);

            var editItem = new MenuFlyoutItem
            {
                Text = "Редактировать",
                Icon = new FontIcon { Glyph = "\xE70F" }
            };
            editItem.Click += (s, e) =>
            {
                if (blockControl.Tag is BlockItem block)
                {
                    _ = ShowEditDialogForBlock(block);
                   
                }
            };

            var info = new MenuFlyoutItem
            {
                Text = "Инфо",
                Icon = new FontIcon { Glyph = "\xE70F" }
            };
            info.Click += (s, e) => ShowBlockInfo(blockControl);

            menu.Items.Add(info);
            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);

            blockControl.ContextFlyout = menu;

           
        }

        private void ShowBlockInfo(BlockItem block)
        {
            var dialog = new ContentDialog
            {
                Title = $"Информация о блоке: {block.Name}",
                Content = block.Description,
                PrimaryButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.ShowAsync();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Централизованный метод для полной очистки блок-схемы
        /// </summary>
        private void ClearFlowchart()
        {
            this.connectionLines.Clear();
            this.listofblocks.Clear();

            var linesToRemove = FlowchartCanvas.Children.OfType<Polyline>().ToList();
            foreach (var line in linesToRemove)
                FlowchartCanvas.Children.Remove(line);

            var arrowsToRemove = FlowchartCanvas.Children.OfType<Polygon>().ToList();
            foreach (var arrow in arrowsToRemove)
                FlowchartCanvas.Children.Remove(arrow);

            var labelsToRemove = BlocksCanvas.Children.OfType<TextBlock>().ToList();
            foreach (var label in labelsToRemove)
                BlocksCanvas.Children.Remove(label);

            BlocksCanvas.Children.Clear();
            ClearVariablePreviewPanels();

            startBlock = null;
            endBlock = null;

            isDebugging = false;
            currentDebugNode = null;
            currentStepIndex = -1;
            executionOrder.Clear();

            ClearBlockHighlights();

            if (undoStack != null) undoStack.Clear();
            if (redoStack != null) redoStack.Clear();
        }

        /// <summary>
        /// Создание новой блок-схемы с подтверждением
        /// </summary>
        private async void NewFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Новая блок-схема",
                Content = "Создать новую блок-схему? Все несохраненные изменения будут утеряны.",
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ClearFlowchart();

                blockCounter = 0;
                InitializeVirtualGrid();
                HighlightAvailableCells();
                if (TraceTextBlock != null)
                    TraceTextBlock.Text = string.Empty;

                ShowNotification("Создана новая блок-схема.");
            }
        }

        private async void OpenFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add(".xml");
            filePicker.FileTypeFilter.Add(".prg");

            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadFlowchartFromFile(file);
            }
        }

        private async void SaveFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeChoices.Add("Блок-схема XML", new List<string> { ".xml", ".prg" });

            StorageFile file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    var dataToSave = new FlowchartData
                    {
                        Blocks = new ObservableCollection<BlockItem>(this.listofblocks),
                        Connections = this.connectionLines
                    };
                    await XmlDataSerializer.SaveToFileAsync(dataToSave, file);

                    ShowNotification($"Блок-схема успешно сохранена в:\n{file.Path}\n\nБлоков: {listofblocks.Count}\nСоединений: {connectionLines.Count}");
                }
                catch (Exception ex)
                {
                    ShowNotification($"Не удалось сохранить блок-схему:\n{ex.Message}");
                }
            }
        }

        private void ShowBlockInfo(Border blockControl)
        {
            if (blockControl.Tag is BlockItem block)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Информация о блоке: {block.Name}",
                    Content = $"{block.Description}\n{block.Docs}",
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                dialog.ShowAsync();
            }
        }

        public void AddBlock(string name, string icon, string description, BlockType type, string docs = null, string shot = null)
        {
            var block = new BlockItem
            {
                Name = name,
                Icon = icon,
                Shot = shot,
                Description = description,
                Docs = docs,
                Type = type
            };
            
            System.Diagnostics.Debug.WriteLine($"Adding block: {name}, Docs={docs ?? "NULL"}, Shot={shot ?? "NULL"}");

            Blocks.Add(block);
        }
        /// <summary>
        /// Вызывается при двойном клике на блоке. Показывает соответствующий диалог редактирования.
        /// </summary>
        public async Task ShowEditDialogForBlock(BlockItem block)
        {
            switch (block.Type)
            {
                case BlockType.While:
                    await ShowWhileEditDialog(block);
                    break;

                case BlockType.DoWhile:
                    await ShowDoWhileEditDialog(block);
                    break;

                case BlockType.For:
                    await ShowForEditDialog(block);
                    break;

                case BlockType.Decision:
                case BlockType.Process:
                case BlockType.Input:
                case BlockType.Output:
                case BlockType.VariableDeclaration:
                case BlockType.ArrayDeclaration:
                    EditWindow.Show(block);
                    break;

                case BlockType.Start:
                case BlockType.End:
                case BlockType.LoopConnector:
                case BlockType.DoLoopConnector:
                    ShowNotification($"Блок '{block.Name}' не может быть отредактирован.");
                    break;

                default:
                    EditWindow.Show(block);
                    break;
            }
            UpdateBlockVisual(block);
        }

        private void StartDebug_Click(object sender, RoutedEventArgs e)
        {
            BuildSyntaxTree();
            if (!InitializeVariables())
            {
                TraceTextBlock.Text += $"\n--- ОШИБКА ИНИЦИАЛИЗАЦИИ. ВЫПОЛНЕНИЕ ПРЕРВАНО. ---";
                return;
            }

            // PseudocodeTextBlock.Text = GenerateCodeFromTree();
            TraceTextBlock.Text = " ";
            if (syntaxTreeRoot == null)
            {
                TraceTextBlock.Text = "Нет стартового блока";
                return;
            }

            executionOrder.Clear();
            if (syntaxTreeRoot != null)
            {
                executionOrder.Add(syntaxTreeRoot);
            }

            if (executionOrder.Count == 0)
            {
                TraceTextBlock.Text = "Нет стартового блока.";
                return;
            }

            isDebugging = true;
            currentStepIndex = 0;
            currentDebugNode = executionOrder[currentStepIndex];

            HighlightCurrentBlock(currentDebugNode.Block);

            BlocksCanvas.Focus(FocusState.Keyboard);

        }

        private void StopDebug_Click(object sender, RoutedEventArgs e)
        {
            isDebugging = false;
            currentDebugNode = null;
            currentStepIndex = -1;
            executionOrder.Clear();
            TraceTextBlock.Text += "\nОтладка остановлена.";

            ClearBlockHighlights();
            ClearVariablePreviewPanels();
        }

        private void StepDebug_Click(object sender, RoutedEventArgs e)
        {
            if (!isDebugging || executionOrder.Count == 0)
            {
                TraceTextBlock.Text = "Отладка не запущена.";
                return;
            }

            if (currentStepIndex >= executionOrder.Count)
            {
                TraceTextBlock.Text += "\nВыполнение завершено.";

                isDebugging = false;
                currentDebugNode = null;
                currentStepIndex = -1;
                executionOrder.Clear();
                TraceTextBlock.Text += "\nОтладка остановлена.";
                ShowTraceResults();
                ClearBlockHighlights();
                return;
            }

            currentDebugNode = executionOrder[currentStepIndex];
            HighlightCurrentBlock(currentDebugNode.Block);
            ScrollToBlock(currentDebugNode.Block);

            TraceTextBlock.Text += $"\n [{currentStepIndex + 1}] {currentDebugNode.Block.Name}";
            bool result = ExecuteBlock(currentDebugNode);
            Tree next = null;

            switch (currentDebugNode.Block.Type)
            {
                case BlockType.Decision:
                    {
                        ConnectionType branch = result switch
                        {
                            true => ConnectionType.TrueBranch,
                            false => ConnectionType.FalseBranch
                        };

                        next = currentDebugNode.Children
                            .FirstOrDefault(c => c.BranchType == branch);
                        break;
                    }

                case BlockType.Loop:
                case BlockType.For:
                case BlockType.While:
                case BlockType.DoWhile:
                    {
                        ConnectionType branch = result ? ConnectionType.TrueBranch : ConnectionType.FalseBranch;
                        next = currentDebugNode.Children.FirstOrDefault(c => c.BranchType == branch);
                        break;
                    }

                case BlockType.LoopConnector:
                case BlockType.DoLoopConnector:
                    {
                        var outgoingConnection = connectionLines
                            .FirstOrDefault(c => c.FromBlock == currentDebugNode.Block);

                        if (outgoingConnection != null)
                        {
                            BlockItem targetBlock = outgoingConnection.ToBlock;

                            next = executionOrder.FirstOrDefault(t => t.Block == targetBlock);

                            if (next == null)
                            {
                                ShowNotification("Ошибка: Не найден оригинальный узел цикла в истории.");
                            }
                        }
                        break;
                    }

                case BlockType.End:
                    next = null;
                    break;

                default:
                    next = currentDebugNode.Children
                        .FirstOrDefault(c => c.BranchType == ConnectionType.Normal);
                    break;
        }
            if (next != null)
                executionOrder.Insert(currentStepIndex + 1, next);
            currentStepIndex++;
        }

        private void SetupBlockDragAndDrop()
        {
            var blocksList = BlocksListView;

            if (blocksList != null)
            {
                blocksList.DragItemsStarting += BlocksList_DragItemsStarting;
            }
        }


        /// <summary>
        /// Обновляет визуальное представление блока после изменения
        /// </summary>
        private void UpdateBlockVisual(BlockItem block)
        {
            var border = BlocksCanvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag == block);

            if (border != null)
            {
                var newBorder = DrawBlock.GetBlock(block);
                newBorder.Tag = block;

                newBorder.PointerPressed += BlockControl_PointerPressed;
                newBorder.PointerReleased += BlockControl_PointerReleased;
                newBorder.DoubleTapped += BlockControl_DoubleTapped;
                AttachAnchorHandlers(newBorder);
                InitializeBlockContextMenu(newBorder);

                Canvas.SetLeft(newBorder, block.CanvasLeft);
                Canvas.SetTop(newBorder, block.CanvasTop);

                int index = BlocksCanvas.Children.IndexOf(border);
                BlocksCanvas.Children.Remove(border);
                BlocksCanvas.Children.Insert(index, newBorder);

                UpdateConnectionLines(block);
            }
        }

        private void InitializeVirtualGrid()
        {
            virtualGrid = new GridNode[GRID_ROWS, GRID_COLUMNS];
            for (int r = 0; r < GRID_ROWS; r++)
                for (int c = 0; c < GRID_COLUMNS; c++)
                    virtualGrid[r, c] = new GridNode { Row = r, Column = c };
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null || settingsWindow.AppWindow == null)
            {
                settingsWindow = new SettingsWindow();

                settingsWindow.SettingsChanged += (s, args) =>
                {
                    ApplySettings();
                };

                settingsWindow.Closed += (s, args) => settingsWindow = null;
            }

            settingsWindow.Activate();
        }

        private void ApplySettings()
        {
            ApplyTheme();

            if (SettingsWindow.AppSettings.ShowGrid)
                DrawGrid(20);
            else
                GridCanvas.Children.Clear();

            highlightRadius = SettingsWindow.AppSettings.HighlightRadius;
            HighlightAvailableCells();
            RecalculateBlockPositions();
            UpdateAutoSaveTimer();
            ShowNotification("Настройки применены успешно!");
        }
        private void UpdateAutoSaveTimer()
        {
            if (autoSaveTimer != null)
            {
                autoSaveTimer.Stop();
                autoSaveTimer = null;
            }

            if (SettingsWindow.AppSettings.AutoSave)
            {
                autoSaveTimer = new DispatcherTimer();
                autoSaveTimer.Interval = TimeSpan.FromMinutes(SettingsWindow.AppSettings.AutoSaveInterval);
                autoSaveTimer.Tick += (s, e) =>
                {
                    AutoSaveFlowchart();
                };
                autoSaveTimer.Start();
            }
        }
    }
}
