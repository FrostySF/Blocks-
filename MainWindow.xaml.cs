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
using Windows.UI;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {  
        public MainWindow()
        {
            InitializeComponent();
            InitializeBlocks();

            InitializeClipboardAndUndo();

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
            this.AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
            this.AppWindow.SetTitleBarIcon(@"Assets/icon.png");
            this.AppWindow.SetTaskbarIcon(@"Assets/icon.icon");
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
        }

        private void InitializeVirtualGrid()
        {
            virtualGrid = new GridNode[GRID_ROWS, GRID_COLUMNS];
            for (int r = 0; r < GRID_ROWS; r++)
            {
                for (int c = 0; c < GRID_COLUMNS; c++)
                {
                    virtualGrid[r, c] = new GridNode { Row = r, Column = c };
                }
            }
        }
        private void InitializeBlocks()
        {
            AddBlock("Начало", "\xE80F", "Начальный блок схемы", BlockType.Start);
            AddBlock("Конец", "\xE8BB", "Конечный блок схемы", BlockType.End);
            AddBlock("Процесс", "\xE909", "Блок обработки данных\n[имя переменной] = [вырожение]", BlockType.Process);
            AddBlock("Описание переменных", "\xE909", "Описание переменных", BlockType.VariableDeclaration);
            AddBlock("Решение", "\xE7EC", "Условный блок (if)", BlockType.Decision);
            AddBlock("Массивы", "\xE8FD", "Объявление векторов и матриц", BlockType.ArrayDeclaration);

            //AddBlock("Цикл", "\xE895", "Блок цикла (for/while)", BlockType.Loop);
            AddBlock("Пока", "\xE895", "Блок цикла (while)", BlockType.While);
            AddBlock("Делай", "\xE895", "Блок цикла (DoWhile)", BlockType.DoWhile);
            AddBlock("Подготовка", "\xE895", "Блок цикла (for)", BlockType.For);
            //AddBlock("circle", "\xE895", "Блок цикла (while)", BlockType.LoopConnector);
            //AddBlock("Ввод/Вывод", "\xE8A5", "Блок ввода/вывода", BlockType.InputOutput);
            AddBlock("Ввод", "\xE8A5", "Блок ввода", BlockType.Input);
            AddBlock("Вывод", "\xE8A5", "Блок вывода", BlockType.Output);
            SetupBlockDragAndDrop();
        }

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
            {
                CreateBlockOnCanvas(block);
            }
        }

        private void CreateBlockOnCanvas(BlockItem templateBlock)
        {
            SaveState();

            if (templateBlock.Type == BlockType.While)
            {
                CreateWhileLoopStructureInViewport();
            }
            if (templateBlock.Type == BlockType.DoWhile)
            {
                CreateDoWhileLoopStructureInViewport();
                return;
            }

            if (templateBlock.Type == BlockType.For)
            {
                CreateForLoopStructureInViewport();
                return;
            }
            if (templateBlock.Type == BlockType.Start || templateBlock.Type == BlockType.End)
            {
                bool startExists = listofblocks.Any(b => b.Type == BlockType.Start);
                bool endExists = listofblocks.Any(b => b.Type == BlockType.End);
                if (startExists && endExists)
                {
                    ShowNotification("Блоки Start и End уже существуют.");
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
                Name = $"{templateBlock.Name} {blockCounter}",
                Icon = templateBlock.Icon,
                Description = templateBlock.Description,
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
            newBlock.CanvasLeft = targetNode.Column * GRID_STEP;
            newBlock.CanvasTop = targetNode.Row * GRID_STEP;
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
        }


        private void Anchor_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (sender is Ellipse anchor && anchor.Tag is (BlockItem block, ConnectionType type))
            {
                var linesToRemove = connectionLines
                    .Where(cl => (cl.FromBlock == block && cl.Type == type) || cl.ToBlock == block)
                    .ToList();

                foreach (var line in linesToRemove)
                {
                    FlowchartCanvas.Children.Remove(line.VisualLine);
                    var arrows = FlowchartCanvas.Children.OfType<Polygon>()
                        .Where(p => Math.Abs(p.Points[0].X - line.VisualLine.X2) < 5 &&
                                   Math.Abs(p.Points[0].Y - line.VisualLine.Y2) < 5)
                        .ToList();
                    foreach (var arrow in arrows)
                    {
                        FlowchartCanvas.Children.Remove(arrow);
                    }

                    connectionLines.Remove(line);
                }

                BuildSyntaxTree();
                e.Handled = true;
            }
        }


        private void AttachAnchorHandlers(Border blockBorder)
        {
            if (blockBorder.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Border hitBox && hitBox.Child is Ellipse anchor)
                    {

                        hitBox.PointerPressed += Anchor_PointerPressed;
                        hitBox.PointerEntered += Anchor_PointerEntered;
                        hitBox.PointerReleased += HitBox_PointerReleased;
                        hitBox.RightTapped += Anchor_RightTapped;
                        hitBox.PointerExited += Anchor_PointerExited;
                    }
                }
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
                    FlowchartCanvas.Children.Remove(polyline);
                    if (connection.ArrowHead != null)
                        FlowchartCanvas.Children.Remove(connection.ArrowHead);

                    connectionLines.Remove(connection);
                    BuildSyntaxTree();
                }

                e.Handled = true;
            }
        }

        private void ClearConnectionLines()
        {
            foreach (var connection in connectionLines)
            {
                FlowchartCanvas.Children.Remove(connection.VisualLine);
            }

            var arrows = FlowchartCanvas.Children.OfType<Polygon>().ToList();
            foreach (var arrow in arrows)
            {
                FlowchartCanvas.Children.Remove(arrow);
            }

            connectionLines.Clear();
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

                if (lineToInsertInto != null)
                {
                    connectionLines.Remove(lineToInsertInto);
                    FlowchartCanvas.Children.Remove(lineToInsertInto.VisualPath);
                    if (lineToInsertInto.ArrowHead != null) FlowchartCanvas.Children.Remove(lineToInsertInto.ArrowHead);
                    CreateManualConnection(lineToInsertInto.FromBlock, selectedBlock, lineToInsertInto.Type);
                    CreateManualConnection(selectedBlock, lineToInsertInto.ToBlock, ConnectionType.Normal);
                }


                int col = (int)Math.Round(selectedBlock.CanvasLeft / (double)GRID_STEP);
                int row = (int)Math.Round(selectedBlock.CanvasTop / (double)GRID_STEP);

                row = Math.Max(0, Math.Min(GRID_ROWS - 1, row));
                col = Math.Max(0, Math.Min(GRID_COLUMNS - 1, col));

                GridNode node = virtualGrid[row, col];

                GridNode desiredNode = node;
                GridNode finalNode = null;

                if (desiredNode.IsAvailable)
                {
                    selectedBlock.CanvasLeft = desiredNode.Column * GRID_STEP;
                    selectedBlock.CanvasTop = desiredNode.Row * GRID_STEP;

                    if (!CheckCollisionAtTemporaryLocation(selectedBlock))
                    {
                        finalNode = desiredNode;
                    }
                }

                if (finalNode == null)
                {
                    finalNode = FindNearestFreeNodeWithoutCollisions(selectedBlock, row, col);
                }


                if (finalNode != null)
                {
                    if (selectedBlock.GridPosition != null)
                    {
                        selectedBlock.GridPosition.OccupiedBy = null;
                    }

                    finalNode.OccupiedBy = selectedBlock;
                    selectedBlock.GridPosition = finalNode;
                    selectedBlock.CanvasLeft = finalNode.Column * GRID_STEP;
                    selectedBlock.CanvasTop = finalNode.Row * GRID_STEP;

                    foreach (var child in BlocksCanvas.Children)
                    {
                        if (child is Border _border && _border.Tag == selectedBlock)
                        {
                            Canvas.SetLeft(_border, selectedBlock.CanvasLeft);
                            Canvas.SetTop(_border, selectedBlock.CanvasTop);
                            break;
                        }
                    }
                }

                UpdateConnectionLines(selectedBlock);
                HighlightAvailableCells();
            }

            selectedBlock = null;

            if (sender is Border border)
            {
                border.ReleasePointerCapture(e.Pointer);
            }
            e.Handled = true;
        }

        private void BlockControl_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is BlockItem block &&
                block.Type != BlockType.Start && block.Type != BlockType.End)
            {
                _ = ShowEditDialogForBlock(block);
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

            if (selectedBlock != null && e.Pointer.IsInContact)
            {
                var currentPosition = e.GetCurrentPoint(FlowchartCanvas).Position;
                var deltaX = currentPosition.X - lastMousePosition.X;
                var deltaY = currentPosition.Y - lastMousePosition.Y;

                selectedBlock.CanvasLeft += deltaX;
                selectedBlock.CanvasTop += deltaY;

                foreach (var child in BlocksCanvas.Children)
                {
                    if (child is Border border && border.Tag == selectedBlock)
                    {
                        Canvas.SetLeft(border, SnapToGrid(selectedBlock.CanvasLeft));
                        Canvas.SetTop(border, SnapToGrid(selectedBlock.CanvasTop));
                        break;
                    }
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
            selectedBlock = null;
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
    }
}