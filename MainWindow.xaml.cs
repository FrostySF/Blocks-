using AutoUpdaterDotNET;
using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        private SettingsWindow settingsWindow;
        private StorageFile currentFile = null;

        public MainWindow()
        {
            
            InitializeComponent();
            InitializeBlocks();

            InitializeClipboardAndUndo();
            UpdateStatusBar();

            FlowchartCanvas.PointerMoved += FlowchartCanvas_PointerMoved;
            FlowchartCanvas.PointerReleased += FlowchartCanvas_PointerReleased;
            FlowchartCanvas.PointerPressed += FlowchartCanvas_PointerPressedForPan;

            FlowchartCanvas.AllowDrop = true;
            FlowchartCanvas.DragOver += FlowchartCanvas_DragOver;
            FlowchartCanvas.Drop += FlowchartCanvas_Drop;
            FlowchartCanvas.DragLeave += FlowchartCanvas_DragLeave;

            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            this.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
            this.AppWindow.TitleBar.ButtonHoverBackgroundColor = Color.FromArgb(20, 255, 255, 255);
            this.AppWindow.TitleBar.ButtonPressedBackgroundColor = Color.FromArgb(30, 255, 255, 255);


            FlowchartCanvas.Loaded += (s, e) => DrawGrid(20);
            FlowchartCanvas.SizeChanged += (s, e) =>
            {
                GridCanvas.Width = FlowchartCanvas.Width;
                GridCanvas.Height = FlowchartCanvas.Height;
                DrawGrid(20);
            };

            if (this.Content is FrameworkElement root)
            {
                root.KeyUp += CoreWindow_KeyUp;
                root.Focus(FocusState.Programmatic);
            }

            InitializeVirtualGrid();
            EditWindow.Content = this.Content;
            ApplySettings();
            InitializeAutoSave();
        }

        private void UpdateStatusBar()
        {
            if (BlockCountTextBlock == null || FileStatusTextBlock == null) return;

            int blockCount = listofblocks?.Count ?? 0; 
            BlockCountTextBlock.Text = $"Блоков: {blockCount}";

            string fileName = currentFile != null ? currentFile.Name : "Нет открытого файла";
            FileStatusTextBlock.Text = $"Файл: {fileName}";
        }
        private void RecalculateBlockPositions()
        {
            int newGridStep = SettingsWindow.AppSettings.GridStep;

            foreach (var block in listofblocks)
                if (block.GridPosition != null)
                {
                    block.CanvasLeft = block.GridPosition.Column * newGridStep;
                    block.CanvasTop = block.GridPosition.Row * newGridStep;

                    var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                    if (border != null)
                    {
                        Canvas.SetLeft(border, block.CanvasLeft);
                        Canvas.SetTop(border, block.CanvasTop);
                    }
                }

            foreach (var block in listofblocks)
                UpdateConnectionLines(block);
        }

        private void InitializeAutoSave()
        {
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

        private async void AutoSaveFlowchart()
        {
            if (currentFile != null)
            {
                await SaveFlowchartData(currentFile);
                ShowNotification($"Автосохранение выполнено в текущий файл: {currentFile.Name}");
            }
            else
            {
                try
                {
                    var folder = ApplicationData.Current.LocalFolder;
                    var autosaveFile = await folder.CreateFileAsync("autosave.prg", CreationCollisionOption.ReplaceExisting);
                    await SaveFlowchartData(autosaveFile); 
                    ShowNotification("Автосохранение выполнено в файл восстановления (autosave.prg)");
                }
                catch (Exception ex)
                {
                    ShowNotification($"Ошибка автосохранения: {ex.Message}");
                }
            }
        }

        private void InitializeBlocks()
        {
            AddBlock("Начало", "\x25CB", "Начальный блок схемы", BlockType.Start, "", "");
            AddBlock("Конец", "\x25CB", "Конечный блок схемы", BlockType.End, "", ""); 
            AddBlock("Присваивание", "\x25AD", "Блок для обработки данных", BlockType.Process,
                "Присваивание\n<Элемент переменной> = <Арифметическое выражение>\n<Элемент таблицы> = <Арифметическое выражение>",
                "Введите арифметическое выражение:");
            AddBlock("Описание", "\x25A3", "Описание переменных", BlockType.VariableDeclaration,
                "Описание переменных\n<Имя переменной> = <НеОбязательноеЗначение>\nПримеры: a=0; x; what=666;",
                "Введите описание переменных:");
            AddBlock("Массивы", "\x25A3", "Объявление векторов и матриц", BlockType.ArrayDeclaration,
                "Массивы\nВектор: <Тип> <Имя>[<Размер>]\nМатрица: <Тип> <Имя>[<Строки>][<Столбцы>]\nПримеры: int arr[10]; double matrix[5][5]",
                "Введите объявление массива:");
            AddBlock("Решение", "\x25C7", "Блок условного оператора", BlockType.Decision,
                "Решение\n<Логическое выражение>\nПримеры: x > 0; a == b; (x > 5) && (y < 10)",
                "Введите логическое выражение:");
            AddBlock("Пока", "\x25C7", "Цикл с предусловием (while)", BlockType.While,
                "Пока\n<Логическое выражение>\nВыполняется пока условие истинно\nПримеры: i < 10; x != 0",
                "Введите логическое выражение:");
            AddBlock("Делай", "\x25C7", "Цикл с постусловием (do-while)", BlockType.DoWhile,
                "Делай-Пока\n<Логическое выражение>\nВыполняется до тех пор, пока условие истинно\nПримеры: choice != 0; continue == true",
                "Введите логическое выражение:");
            AddBlock("Подготовка", "\x2B21", "Цикл со счётчиком (for)", BlockType.For,
                "Подготовка (for)\n<Переменная> от <Начальное значение> до <Конечное значение> шаг <Приращение>\nПримеры: i от 1 до 10 шаг 1; x от 0 до 100 шаг 5",
                "Введите параметры цикла:");
            AddBlock("Ввод", "\x25B1", "Блок ввода данных", BlockType.Input,
                "Ввод\n<Список переменных через запятую>\nПримеры: x, y, z; name, age",
                "Введите список ввода:");
            AddBlock("Вывод", "\x25B1", "Блок вывода данных", BlockType.Output,
                "Вывод\n<Список выражений через запятую>\nПримеры: x, y; \"Результат:\", result; a + b",
                "Введите список вывода:");

            SetupBlockDragAndDrop();
        }
        #region Split Panel Handlers

        private void LeftSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isLeftSplitterDragging = true;
            splitterStartX = e.GetCurrentPoint(this.Content as UIElement).Position.X;
            ((Border)sender).CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void LeftSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isLeftSplitterDragging)
            {
                var currentX = e.GetCurrentPoint(this.Content as UIElement).Position.X;
                var delta = currentX - splitterStartX;

                var splitterBorder = (Border)sender;
                var parentGrid = splitterBorder.Parent as Grid;

                if (parentGrid != null)
                {
                    var leftColumn = parentGrid.ColumnDefinitions[0];
                    var newWidth = leftColumn.Width.Value + delta;

                    if (newWidth >= 150 && newWidth <= 400)
                    {
                        leftColumn.Width = new GridLength(newWidth);
                        splitterStartX = currentX;
                    }
                }
            }
        }

        private void LeftSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isLeftSplitterDragging = false;
            ((Border)sender).ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void RightSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            isRightSplitterDragging = true;
            splitterStartX = e.GetCurrentPoint(this.Content as UIElement).Position.X;
            ((Border)sender).CapturePointer(e.Pointer);
            e.Handled = true;
        }

        private void RightSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isRightSplitterDragging)
            {
                var currentX = e.GetCurrentPoint(this.Content as UIElement).Position.X;
                var delta = splitterStartX - currentX;

                var splitterBorder = (Border)sender;
                var parentGrid = splitterBorder.Parent as Grid;

                if (parentGrid != null)
                {
                    var rightColumn = parentGrid.ColumnDefinitions[4];
                    var newWidth = rightColumn.Width.Value + delta;

                    if (newWidth >= 150 && newWidth <= 400)
                    {
                        rightColumn.Width = new GridLength(newWidth);
                        splitterStartX = currentX;
                    }
                }
            }
        }

        private void RightSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            isRightSplitterDragging = false;
            ((Border)sender).ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void Splitter_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Устанавливаем курсор изменения размера
                //border.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.SizeWestEast);

                // Подсвечиваем разделитель
                if (border.Child is Rectangle rect)
                {
                    rect.Fill = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255));
                }
            }
        }

        private void Splitter_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border)
            {
                // Возвращаем обычный курсор
                //border.ProtectedCursor = Microsoft.UI.Input.InputSystemCursor.Create(Microsoft.UI.Input.InputSystemCursorShape.Arrow);

                // Убираем подсветку
                if (border.Child is Rectangle rect && !isLeftSplitterDragging && !isRightSplitterDragging)
                {
                    rect.Fill = new SolidColorBrush(Color.FromArgb(32, 255, 255, 255));
                }
            }
        }

        #endregion

        private void BlocksList_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            if (e.Items.Count > 0 && e.Items[0] is BlockItem block)
            {
                draggedBlockTemplate = block;
                isDraggingFromPanel = true;
                e.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            }
        }


        private void BlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BlockItem block)
                CreateBlockOnCanvas(block);
        }

        private void CreateBlockOnCanvas(BlockItem templateBlock)
        {
            SaveState();

            if (templateBlock.Type == BlockType.While)
                CreateWhileLoopStructureInViewport(templateBlock.Shot, templateBlock.Docs);
            if (templateBlock.Type == BlockType.DoWhile)
            {
                CreateDoWhileLoopStructureInViewport(templateBlock.Shot, templateBlock.Docs);
                return;
            }

            if (templateBlock.Type == BlockType.For)
            {
                CreateForLoopStructureInViewport(templateBlock.Docs, templateBlock.Shot);
                return;
            }
            if (templateBlock.Type == BlockType.Start || templateBlock.Type == BlockType.End)
            {
                bool startExists = listofblocks.Any(b => b.Type == BlockType.Start);
                bool endExists = listofblocks.Any(b => b.Type == BlockType.End);
                if (startExists && endExists)
                {
                    ShowNotification("Блоки Начало и Конец уже существуют.");
                    return;
                }
                if (!startExists && !endExists)
                {
                    if (templateBlock.Type == BlockType.Start)
                        CreateBeginEndStructureInViewport();

                    return;
                }

                if ((templateBlock.Type == BlockType.Start && startExists) ||
                    (templateBlock.Type == BlockType.End && endExists))
                {
                    ShowNotification($"Блок {templateBlock.Type} уже существует.");
                    return;
                }
            }
            blockCounter++;

            var newBlock = new BlockItem
            {
                Name = $"{templateBlock.Name}",
                Icon = templateBlock.Icon,
                Description = templateBlock.Description,
                Shot = templateBlock.Shot,
                Docs = templateBlock.Docs,
                Type = templateBlock.Type,
                Id = Guid.NewGuid()
            };

            foreach (var block in listofblocks)
            {
                if (block.Type == newBlock.Type &&
                    (templateBlock.Type == BlockType.Start || templateBlock.Type == BlockType.End))
                {
                    if (this.Content?.XamlRoot == null) return;
                    ShowNotification("Данный блок может быть только один.");
                    blockCounter--;
                    return;
                }
            }

            GridNode targetNode = FindFirstFreeGridNodeInViewport();
            if (targetNode == null)
            {
                ShowNotification("Свободные ячейки для блоков отсутствуют.");
                return;
            }

            targetNode.OccupiedBy = newBlock;
            newBlock.CanvasLeft = targetNode.Column * SettingsWindow.AppSettings.GridStep;
            newBlock.CanvasTop = targetNode.Row * SettingsWindow.AppSettings.GridStep;
            newBlock.GridPosition = targetNode;

            if (newBlock.Type == BlockType.Start) startBlock = newBlock;
            else if (newBlock.Type == BlockType.End) endBlock = newBlock;

            Border border = DrawBlock.GetBlock(newBlock);
            border.Tag = newBlock;

            border.PointerPressed += BlockControl_PointerPressed;
            border.PointerReleased += BlockControl_PointerReleased;
            border.DoubleTapped += BlockControl_DoubleTapped;

            AttachAnchorHandlers(border);
            InitializeBlockContextMenu(border);

            Canvas.SetLeft(border, newBlock.CanvasLeft);
            Canvas.SetTop(border, newBlock.CanvasTop);
            BlocksCanvas.Children.Add(border);

            listofblocks.Add(newBlock);

            HighlightAvailableCells();
            UpdateStatusBar();
        }


        private void Anchor_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Ellipse anchor && anchor.Tag is (BlockItem block, ConnectionType type))
            {
                var linesToRemove = connectionLines
                    .Where(cl => (cl.FromBlock == block && cl.Type == type) || cl.ToBlock == block)
                    .ToList();

                foreach (var line in linesToRemove)
                    RemoveConnection(line);
                BuildSyntaxTree();
                e.Handled = true;
            }
        }


        private void AttachAnchorHandlers(Border blockBorder)
        {
            if (blockBorder.Child is Grid grid)

                foreach (var child in grid.Children)
                    if (child is Border hitBox && hitBox.Child is Ellipse anchor)
                    {

                        hitBox.PointerPressed += Anchor_PointerPressed;
                        hitBox.PointerEntered += Anchor_PointerEntered;
                        hitBox.PointerReleased += HitBox_PointerReleased;
                        hitBox.RightTapped += Anchor_RightTapped;
                        hitBox.PointerExited += Anchor_PointerExited;
                    }
        }

        private void HitBox_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                return;

            if (sender is Border hitBox && hitBox.Tag is ValueTuple<BlockItem, ConnectionType> tag)
            {
                var (block, type) = tag;
                if (selectedAnchor == null)
                {
                    selectedAnchor = hitBox.Child as Ellipse;
                    connectionStartBlock = block;
                    connectionStartType = type;

                    var point = e.GetCurrentPoint(FlowchartCanvas).Position;
                    previewLine = new Line
                    {
                        X1 = block.CanvasLeft + 50,
                        Y1 = block.CanvasTop + 30,
                        X2 = point.X,
                        Y2 = point.Y,
                        Stroke = new SolidColorBrush(Colors.Yellow),
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 5 }
                    };
                    FlowchartCanvas.Children.Add(previewLine);
                }
                else
                {
                    if (block != connectionStartBlock)
                    {
                        CreateManualConnection(connectionStartBlock, block, connectionStartType);
                    }

                    if (previewLine != null)
                    {
                        FlowchartCanvas.Children.Remove(previewLine);
                        previewLine = null;
                    }

                    selectedAnchor = null;
                    connectionStartBlock = null;
                }

                e.Handled = true;
            }
        }

        private void Anchor_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.GetCurrentPoint(null).Properties.IsRightButtonPressed)
                return;

            if (sender is Border hitBox && hitBox.Tag is ValueTuple<BlockItem, ConnectionType> tag)
            {
                var (block, type) = tag;
                if (selectedAnchor == null)
                {
                    selectedAnchor = hitBox.Child as Ellipse;
                    connectionStartBlock = block;
                    connectionStartType = type;

                    var point = e.GetCurrentPoint(FlowchartCanvas).Position;
                    previewLine = new Line
                    {
                        X1 = GetAnchorPosition(connectionStartBlock, connectionStartType, true).X,
                        Y1 = GetAnchorPosition(connectionStartBlock, connectionStartType, true).Y,
                        X2 = point.X,
                        Y2 = point.Y,
                        Stroke = new SolidColorBrush(Colors.Yellow),
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 5, 5 }
                    };
                    FlowchartCanvas.Children.Add(previewLine);
                }
                else
                {
                    if (block != connectionStartBlock)
                    {
                        CreateManualConnection(connectionStartBlock, block, connectionStartType);
                    }

                    if (previewLine != null)
                    {
                        FlowchartCanvas.Children.Remove(previewLine);
                        previewLine = null;
                    }

                    selectedAnchor = null;
                    connectionStartBlock = null;
                }

                e.Handled = true;
            }
        }


        private void Anchor_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border hitBox && hitBox.Child is Ellipse anchor)
            {
                anchor.Width = 16;
                anchor.Height = 16;
            }
        }

        private void Anchor_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border hitBox && hitBox.Child is Ellipse anchor && anchor != selectedAnchor)
            {
                anchor.Width = 10;
                anchor.Height = 10;
            }
        }

       
        private void BuildSyntaxTree()
        {
            if (startBlock == null) return;

            syntaxTreeRoot = new Tree { Block = startBlock };
            var visited = new HashSet<BlockItem>();
            BuildTreeRecursive(syntaxTreeRoot, startBlock, visited);
        }

        private void BuildTreeRecursive(Tree parentNode, BlockItem current, HashSet<BlockItem> visited)
        {
            if (visited.Contains(current)) return;
            visited.Add(current);

            var outgoing = connectionLines
                .Where(c => c.FromBlock == current)
                .OrderBy(c => c.Type)
                .ToList();

            foreach (var conn in outgoing)
            {
                var childNode = new Tree
                {
                    Block = conn.ToBlock,
                    Parent = parentNode,
                    BranchType = conn.Type
                };
                parentNode.Children.Add(childNode);
                BuildTreeRecursive(childNode, conn.ToBlock, visited);
            }
        }
        private void Polyline_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Polyline polyline)
            {
                var connection = connectionLines.FirstOrDefault(cl => cl.VisualPath == polyline);
                if (connection != null)
                {
                    RemoveConnection(connection);
                    BuildSyntaxTree();
                }

                e.Handled = true;
            }
        }

        private void BlockControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is BlockItem block)
            {
                selectedBlock = block;
                lastMousePosition = e.GetCurrentPoint(FlowchartCanvas).Position;
                border.CapturePointer(e.Pointer);

                if (block.GridPosition != null)
                {
                    virtualGrid[block.GridPosition.Row, block.GridPosition.Column].OccupiedBy = null;
                    block.GridPosition = null;
                    HighlightAvailableCells();
                }

                e.Handled = true;
            }
        }

        private void BlockControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (selectedBlock != null)
            {
                Rect blockRect = new Rect(selectedBlock.CanvasLeft, selectedBlock.CanvasTop, 100, 60);

                ConnectionLine lineToInsertInto = null;

                foreach (var connection in connectionLines.Where(cl => cl.VisualPath != null))
                {
                    for (int i = 0; i < connection.VisualPath.Points.Count - 1; i++)
                    {
                        Point p1 = connection.VisualPath.Points[i];
                        Point p2 = connection.VisualPath.Points[i + 1];

                        if (IsLineSegmentCloseToRect(p1, p2, blockRect, 30))
                        {
                            lineToInsertInto = connection;
                            break;
                        }
                    }
                    if (lineToInsertInto != null) break;
                }


                int col = (int)Math.Round(selectedBlock.CanvasLeft / (double)SettingsWindow.AppSettings.GridStep);
                int row = (int)Math.Round(selectedBlock.CanvasTop / (double)SettingsWindow.AppSettings.GridStep);

                row = Math.Max(0, Math.Min(GRID_ROWS - 1, row));
                col = Math.Max(0, Math.Min(GRID_COLUMNS - 1, col));

                GridNode node = virtualGrid[row, col];

                GridNode desiredNode = node;
                GridNode finalNode = null;

                if (desiredNode.IsAvailable)
                {
                    selectedBlock.CanvasLeft = desiredNode.Column * SettingsWindow.AppSettings.GridStep;
                    selectedBlock.CanvasTop = desiredNode.Row * SettingsWindow.AppSettings.GridStep;

                    if (!CheckCollisionAtTemporaryLocation(selectedBlock))
                        finalNode = desiredNode;
                }

                if (finalNode == null)
                    finalNode = FindNearestFreeNodeWithoutCollisions(selectedBlock, row, col);

                if (finalNode != null)
                {
                    if (selectedBlock.GridPosition != null)
                        selectedBlock.GridPosition.OccupiedBy = null;

                    finalNode.OccupiedBy = selectedBlock;
                    selectedBlock.GridPosition = finalNode;
                    selectedBlock.CanvasLeft = finalNode.Column * SettingsWindow.AppSettings.GridStep;
                    selectedBlock.CanvasTop = finalNode.Row * SettingsWindow.AppSettings.GridStep;

                    foreach (var child in BlocksCanvas.Children)
                        if (child is Border _border && _border.Tag == selectedBlock)
                        {
                            Canvas.SetLeft(_border, selectedBlock.CanvasLeft);
                            Canvas.SetTop(_border, selectedBlock.CanvasTop);
                            break;
                        }
                }

                UpdateConnectionLines(selectedBlock);
                HighlightAvailableCells();
            }

            selectedBlock = null;

            if (sender is Border border)
                border.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }

        private void BlockControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is BlockItem block &&
                block.Type != BlockType.Start && block.Type != BlockType.End)
            {
                _ = ShowEditDialogForBlock(block);
                UpdateBlockVisual(block);
            }
            e.Handled = true;
        }

        private void FlowchartCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (previewLine != null)
            {
                var point = e.GetCurrentPoint(FlowchartCanvas).Position;
                previewLine.X2 = point.X;
                previewLine.Y2 = point.Y;
            }

            if (isPanning && e.Pointer.IsInContact)
            {
                var current = e.GetCurrentPoint(MainScrollViewer).Position;
                double deltaX = (lastPanPosition.X - current.X) * PanningSensitivity;
                double deltaY = (lastPanPosition.Y - current.Y) * PanningSensitivity;

                MainScrollViewer.ChangeView(
                    MainScrollViewer.HorizontalOffset + deltaX,
                    MainScrollViewer.VerticalOffset + deltaY,
                    null);

                lastPanPosition = current;
                e.Handled = true;
                return;
            }

            if (isDraggingSelection && selectedBlocks.Count > 0 && e.Pointer.IsInContact)
            {
                var currentPosition = e.GetCurrentPoint(FlowchartCanvas).Position;
                var deltaX = currentPosition.X - lastMousePosition.X;
                var deltaY = currentPosition.Y - lastMousePosition.Y;
                foreach (var block in selectedBlocks)
                {
                    block.CanvasLeft += deltaX;
                    block.CanvasTop += deltaY;

                    var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                    if (border != null)
                    {
                        Canvas.SetLeft(border, SnapToGrid(block.CanvasLeft));
                        Canvas.SetTop(border, SnapToGrid(block.CanvasTop));
                    }

                    UpdateConnectionLines(block);
                }

                lastMousePosition = currentPosition;
                e.Handled = true;
                return;
            }

            if (selectedBlock != null && e.Pointer.IsInContact && !isDraggingSelection)
            {
                var currentPosition = e.GetCurrentPoint(FlowchartCanvas).Position;
                var deltaX = currentPosition.X - lastMousePosition.X;
                var deltaY = currentPosition.Y - lastMousePosition.Y;

                selectedBlock.CanvasLeft += deltaX;
                selectedBlock.CanvasTop += deltaY;

                foreach (var child in BlocksCanvas.Children)
                    if (child is Border border && border.Tag == selectedBlock)
                    {
                        Canvas.SetLeft(border, SnapToGrid(selectedBlock.CanvasLeft));
                        Canvas.SetTop(border, SnapToGrid(selectedBlock.CanvasTop));
                        break;
                    }

                UpdateConnectionLines(selectedBlock);
                lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        private void FlowchartCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isPanning)
            {
                FlowchartCanvas.ReleasePointerCapture(e.Pointer);
                isPanning = false;
            }

            if (previewLine != null)
            {
                FlowchartCanvas.Children.Remove(previewLine);
                previewLine = null;
                selectedAnchor = null;
                connectionStartBlock = null;
            }

            isDraggingSelection = false;
            selectedBlock = null;
            clickedBlock = null;
        }

        private void HighlightCurrentBlock(BlockItem block)
        {
            ClearBlockHighlights();

            foreach (var child in BlocksCanvas.Children)
            {
                if (child is Border border && border.Tag == block)
                {
                    highlightedBorder = border;
                    if (border.Child is Grid grid)
                    {
                        foreach (var element in grid.Children)
                        {
                            if (element is Shape shape)
                            {
                                var glowBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(128, 255, 255, 0));
                                shape.Stroke = glowBrush;
                                shape.StrokeThickness = 4;
                                DoubleAnimation blinkAnimation = new DoubleAnimation
                                {
                                    From = 0.8,
                                    To = 1.0,
                                    Duration = new Duration(TimeSpan.FromSeconds(0.6)),
                                    AutoReverse = true,
                                    RepeatBehavior = RepeatBehavior.Forever
                                };

                                highlightStoryboard = new Storyboard();
                                Storyboard.SetTarget(blinkAnimation, shape);
                                Storyboard.SetTargetProperty(blinkAnimation, "Opacity");
                                highlightStoryboard.Children.Add(blinkAnimation);

                                highlightStoryboard.Begin();
                                break;
                            }
                        }
                    }

                    break;
                }
            }
        }


        private void ClearBlockHighlights()
        {
            if (highlightStoryboard != null)
            {
                highlightStoryboard.Stop();
                highlightStoryboard = null;
            }

            foreach (var child in BlocksCanvas.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    foreach (var element in grid.Children)
                    {
                        if (element is Shape shape)
                        {
                            shape.Stroke = new SolidColorBrush(Colors.White);
                            shape.StrokeThickness = 2;
                            shape.Opacity = 1.0;
                        }
                    }
                }
            }

            highlightedBorder = null;
        }
        private void ApplyTheme()
        {
            string theme = SettingsWindow.AppSettings.Theme;
            string accentColor = SettingsWindow.AppSettings.AccentColor;

            var elementTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };

            if (this.Content is FrameworkElement rootElement)
                rootElement.RequestedTheme = elementTheme;
            UpdateTitleBarColors(elementTheme);
            Color color = accentColor switch
            {
                "Blue" => Color.FromArgb(255, 0, 120, 215),
                "Green" => Color.FromArgb(255, 16, 124, 16),
                "Purple" => Color.FromArgb(255, 136, 23, 152),
                "Red" => Color.FromArgb(255, 232, 17, 35),
                "Orange" => Color.FromArgb(255, 247, 99, 12),
                _ => Color.FromArgb(255, 0, 120, 215)
            };

            try
            {
                if (Application.Current.Resources.ContainsKey("SystemAccentColor"))
                    Application.Current.Resources["SystemAccentColor"] = color;
            }
            catch { }

            DrawGrid();
        }

        private void UpdateTitleBarColors(ElementTheme theme)
        {
            if (theme == ElementTheme.Light)
            {
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                this.AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                this.AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.Black;
            }
            else
            {
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                this.AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
                this.AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;
            }
        }

    }
}