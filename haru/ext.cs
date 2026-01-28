using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        private bool CheckPathBlockIntersection(List<Point> pathPoints, BlockItem excludeBlock1, BlockItem excludeBlock2)
        {
            var blockRects = Blocks
                .Where(b => b != excludeBlock1 && b != excludeBlock2)
                .Select(b => new Rect(b.CanvasLeft, b.CanvasTop, 100, 60))
                .ToList();

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Point p1 = pathPoints[i];
                Point p2 = pathPoints[i + 1];

                double x1 = Math.Min(p1.X, p2.X);
                double x2 = Math.Max(p1.X, p2.X);
                double y1 = Math.Min(p1.Y, p2.Y);
                double y2 = Math.Max(p1.Y, p2.Y);

                foreach (var rect in blockRects)
                    if (Math.Abs(p1.X - p2.X) < 1.0)
                        if (p1.X > rect.Left && p1.X < rect.Right)
                            if (y1 < rect.Bottom && y2 > rect.Top)
                                return true;
                    else if (Math.Abs(p1.Y - p2.Y) < 1.0)
                        if (p1.Y > rect.Top && p1.Y < rect.Bottom)
                            if (x1 < rect.Right && x2 > rect.Left)
                                return true;
            }
            return false;
        }

        private bool CheckPathLineOverlap(List<Point> pathPoints, ConnectionLine currentConnection)
        {
            foreach (var otherConnection in connectionLines.Where(c => c != currentConnection && c.VisualPath != null))
            {
                if (otherConnection.VisualPath.Points.Count < 2) continue;

                for (int i = 0; i < pathPoints.Count - 1; i++)
                {
                    Point currentP1 = pathPoints[i];
                    Point currentP2 = pathPoints[i + 1];

                    double currentX1 = Math.Min(currentP1.X, currentP2.X);
                    double currentX2 = Math.Max(currentP1.X, currentP2.X);
                    double currentY1 = Math.Min(currentP1.Y, currentP2.Y);
                    double currentY2 = Math.Max(currentP1.Y, currentP2.Y);

                    for (int j = 0; j < otherConnection.VisualPath.Points.Count - 1; j++)
                    {
                        Point otherP1 = otherConnection.VisualPath.Points[j];
                        Point otherP2 = otherConnection.VisualPath.Points[j + 1];
                        if (Math.Abs(currentP1.X - currentP2.X) < 1.0 && Math.Abs(otherP1.X - otherP2.X) < 1.0)
                            if (Math.Abs(currentP1.X - otherP1.X) < 5)
                                if (Math.Max(currentY1, Math.Min(otherP1.Y, otherP2.Y)) < Math.Min(currentY2, Math.Max(otherP1.Y, otherP2.Y)))
                                    return true;
                        else if (Math.Abs(currentP1.Y - currentP2.Y) < 1.0 && Math.Abs(otherP1.Y - otherP2.Y) < 1.0)
                            if (Math.Abs(currentP1.Y - otherP1.Y) < 5)
                                if (Math.Max(currentX1, Math.Min(otherP1.X, otherP2.X)) < Math.Min(currentX2, Math.Max(otherP1.X, otherP2.X)))
                                    return true;
                    }
                }
            }
            return false;
        }
       
        private void FlowchartCanvas_PointerPressedForPan(object sender, PointerRoutedEventArgs e)
        {
            if (isSpaceBarPressed && e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
            {
                if (selectedBlock == null)
                {
                    isPanning = true;
                    lastPanPosition = e.GetCurrentPoint(MainScrollViewer).Position;
                    FlowchartCanvas.CapturePointer(e.Pointer);
                    e.Handled = true;
                }
            }
        }

        private void CoreWindow_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceBarPressed = false;
                if (isPanning)
                {
                    isPanning = false;
                }

                e.Handled = true;
            }
        }
        private ElementTheme GetCurrentTheme()
        {
            if (this.Content is FrameworkElement rootElement)
            {
                return rootElement.ActualTheme;
            }
            return ElementTheme.Default;
        }

        private Color GetGridColor()
        {
            var theme = GetCurrentTheme();

            if (theme == ElementTheme.Light)
            {
                return Color.FromArgb(255, 0, 0, 0);
            }
            else
            {
                return Color.FromArgb(95, 95, 95, 95);
            }
        }

        private void DrawGrid(double step = 20, double thickness = 0.6)
        {
            GridCanvas.Children.Clear();
            double width = FlowchartCanvas.Width;
            double height = FlowchartCanvas.Height;

            var gridColor = GetGridColor();
            for (double x = 0; x <= width; x += step)
            {
                var line = new Line
                {
                    X1 = x,
                    Y1 = 0,
                    X2 = x,
                    Y2 = height,
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = thickness,
                    Opacity = 0.12,
                    IsHitTestVisible = false
                };
                GridCanvas.Children.Add(line);
            }

            // Горизонтальные линии
            for (double y = 0; y <= height; y += step)
            {
                var line = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = width,
                    Y2 = y,
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = thickness,
                    Opacity = 0.12,
                    IsHitTestVisible = false
                };
                GridCanvas.Children.Add(line);
            }
        }


        private void HighlightAvailableCells()
        {
            foreach (var rect in gridHighlights) GridCanvas.Children.Remove(rect);
            gridHighlights.Clear();

            var candidates = new HashSet<GridNode>();

            if (!listofblocks.Any())
            {
                var center = virtualGrid[2, 2];
                if (center.IsAvailable) candidates.Add(center);
            }
            else
            {
                foreach (var block in listofblocks)
                {
                    var pos = block.GridPosition;
                    if (pos == null) continue;

                    for (int dr = -highlightRadius; dr <= highlightRadius; dr++)
                        for (int dc = -highlightRadius; dc <= highlightRadius; dc++)
                        {
                            int nr = pos.Row + dr;
                            int nc = pos.Column + dc;
                            if (nr < 0 || nr >= GRID_ROWS || nc < 0 || nc >= GRID_COLUMNS) continue;
                            var node = virtualGrid[nr, nc];
                            if (node.IsAvailable) candidates.Add(node);
                        }
                }
            }

            foreach (var node in candidates)
            {
                var rect = new Rectangle
                {
                    Width = SettingsWindow.AppSettings.GridStep,
                    Height = SettingsWindow.AppSettings.GridStep,
                    Stroke = new SolidColorBrush(Color.FromArgb(160, 144, 238, 144)),
                    StrokeThickness = 1,
                    Opacity = 0.25,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(rect, node.Column * SettingsWindow.AppSettings.GridStep);
                Canvas.SetTop(rect, node.Row * SettingsWindow.AppSettings.GridStep);
                GridCanvas.Children.Add(rect);
                gridHighlights.Add(rect);
            }
        }
        private void DeleteBlock(Border blockControl)
        {
            SaveState();
            if (blockControl.Tag is BlockItem block)
            {
                var incomingConnections = connectionLines.Where(cl => cl.ToBlock == block).ToList();
                var outgoingConnections = connectionLines.Where(cl => cl.FromBlock == block).ToList();

                BlockItem newFromBlock = null;
                BlockItem newToBlock = null;
                ConnectionType? connectionTypeToRestore = null;

                if (incomingConnections.Count == 1 && outgoingConnections.Count == 1)
                {
                    var connIn = incomingConnections[0];
                    var connOut = outgoingConnections[0];

                    if (connIn.Type == connOut.Type)
                    {
                        newFromBlock = connIn.FromBlock;
                        newToBlock = connOut.ToBlock;
                        connectionTypeToRestore = connIn.Type;
                    }
                }

                var linesToRemove = incomingConnections.Concat(outgoingConnections).ToList();
                foreach (var line in linesToRemove)
                    RemoveConnection(line);
                if (newFromBlock != null && newToBlock != null && connectionTypeToRestore.HasValue)
                    CreateManualConnection(newFromBlock, newToBlock, connectionTypeToRestore.Value);
                listofblocks.Remove(block);
                if (block.Type == BlockType.Start)
                    startBlock = null;
                else if (block.Type == BlockType.End)
                    endBlock = null;
                if (block.GridPosition != null)
                    virtualGrid[block.GridPosition.Row, block.GridPosition.Column].OccupiedBy = null;
                BlocksCanvas.Children.Remove(blockControl);
                if (blockVariablePanels.TryGetValue(block, out Border previewBorder))
                {
                    BlocksCanvas.Children.Remove(previewBorder);
                    blockVariablePanels.Remove(block);
                }
                BuildSyntaxTree();
                HighlightAvailableCells();
            }
            UpdateStatusBar();
        }

        public async Task LoadFlowchartFromFile(StorageFile file)
        {
            this.connectionLines.Clear();
            this.BlocksCanvas.Children.Clear();
            this.listofblocks.Clear();

            var linesToRemove = FlowchartCanvas.Children.OfType<Polyline>().ToList();
            foreach (var line in linesToRemove)
                FlowchartCanvas.Children.Remove(line);

            var arrowsToRemove = FlowchartCanvas.Children.OfType<Polygon>().ToList();
            foreach (var arrow in arrowsToRemove)
                FlowchartCanvas.Children.Remove(arrow);

            var labelsToRemove = BlocksCanvas.Children.OfType<TextBlock>()
                .Where(tb => connectionLines.Any(c => c.VisualLabel == tb))
                .ToList();
            foreach (var label in labelsToRemove)
                BlocksCanvas.Children.Remove(label);

            startBlock = null;
            endBlock = null;

            InitializeVirtualGrid();

            try
            {
                var loadedData = await XmlDataSerializer.LoadFromFileAsync<FlowchartData>(file);

                foreach (var block in loadedData.Blocks)
                {
                    this.listofblocks.Add(block);
                    if (block.Type == BlockType.Start)
                        startBlock = block;
                    else if (block.Type == BlockType.End)
                        endBlock = block;

                    int col = (int)Math.Round(block.CanvasLeft / (double)SettingsWindow.AppSettings.GridStep);
                    int row = (int)Math.Round(block.CanvasTop / (double)SettingsWindow.AppSettings.GridStep);

                    row = Math.Max(0, Math.Min(GRID_ROWS - 1, row));
                    col = Math.Max(0, Math.Min(GRID_COLUMNS - 1, col));

                    virtualGrid[row, col].OccupiedBy = block;
                    block.GridPosition = virtualGrid[row, col];

                    Border border = DrawBlock.GetBlock(block);
                    border.Tag = block;

                    border.PointerPressed += BlockControl_PointerPressed;
                    border.PointerReleased += BlockControl_PointerReleased;
                    border.DoubleTapped += BlockControl_DoubleTapped;
                    AttachAnchorHandlers(border);
                    InitializeBlockContextMenu(border);

                    Canvas.SetLeft(border, block.CanvasLeft);
                    Canvas.SetTop(border, block.CanvasTop);
                    BlocksCanvas.Children.Add(border);
                }

                int loadedConnections = 0;
                int skippedConnections = 0;
                foreach (var connection in loadedData.Connections)
                {
                    var fromBlock = this.listofblocks.FirstOrDefault(b => b.Id == connection.FromBlockId);
                    var toBlock = this.listofblocks.FirstOrDefault(b => b.Id == connection.ToBlockId);

                    if (fromBlock == null || toBlock == null)
                    {
                        skippedConnections++;
                        System.Diagnostics.Debug.WriteLine($"Пропущено соединение: FromBlockId={connection.FromBlockId}, ToBlockId={connection.ToBlockId}");
                        continue;
                    }
                    connection.FromBlock = fromBlock;
                    connection.ToBlock = toBlock;

                    ConnectionType endAnchorType = ConnectionType.Input;
                    ConnectionType routingType = connection.Type;
                    if (connection.FromBlock.Type == BlockType.LoopConnector &&
                        (connection.ToBlock.Type == BlockType.While ||
                         connection.ToBlock.Type == BlockType.DoWhile ||
                         connection.ToBlock.Type == BlockType.For))
                    {
                        endAnchorType = ConnectionType.LoopBody;
                        routingType = ConnectionType.LoopBody;
                    }

                    Point startAnchor = GetAnchorPosition(fromBlock, connection.Type, isOutput: true);
                    Point endAnchor = GetAnchorPosition(toBlock, endAnchorType, isOutput: false);

                    var pathPoints = RoutePath(startAnchor, endAnchor, routingType);
                    var baseColor = connection.Type switch
                    {
                        ConnectionType.TrueBranch => Colors.LimeGreen,
                        ConnectionType.FalseBranch => Colors.IndianRed,
                        ConnectionType.LoopBody => Colors.DeepSkyBlue,
                        _ => Colors.White
                    };

                    var polyline = new Polyline
                    {
                        Stroke = new SolidColorBrush(baseColor),
                        StrokeThickness = 2,
                        StrokeLineJoin = PenLineJoin.Round
                    };

                    polyline.Points = new PointCollection();
                    foreach (var p in pathPoints)
                        polyline.Points.Add(p);

                    polyline.RightTapped += Polyline_RightTapped;

                    bool intersectsBlock = CheckPathBlockIntersection(pathPoints, fromBlock, toBlock);
                    bool overlapsLine = CheckPathLineOverlap(pathPoints, connection);

                    if (intersectsBlock || overlapsLine)
                    {
                        polyline.Stroke = ErrorLineColor;
                    }

                    FlowchartCanvas.Children.Add(polyline);
                    connection.VisualPath = polyline;
                    connection.Points = pathPoints;
                    connection.Stroke = polyline.Stroke as SolidColorBrush;

                    var arrowColor = (polyline.Stroke as SolidColorBrush)?.Color ?? Colors.White;
                    var arrow = CreateArrowHeadForPath(pathPoints, arrowColor);

                    if (arrow != null)
                    {
                        FlowchartCanvas.Children.Add(arrow);
                        connection.ArrowHead = arrow;
                    }

                    if (connection.Type == ConnectionType.TrueBranch || connection.Type == ConnectionType.FalseBranch)
                    {
                        string labelText = connection.Type == ConnectionType.TrueBranch ? "Да" : "Нет";
                        var label = new TextBlock
                        {
                            Text = labelText,
                            Foreground = polyline.Stroke,
                            FontSize = 14,
                            FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                            IsHitTestVisible = false
                        };

                        if (pathPoints.Count > 0)
                        {
                            Point anchorPoint = pathPoints[0];
                            double offsetX = (connection.Type == ConnectionType.TrueBranch) ? 8.0 : -33.0;

                            if (connection.Type == ConnectionType.FalseBranch && fromBlock.Type == BlockType.While)
                                offsetX = 8.0;

                            Canvas.SetLeft(label, anchorPoint.X + offsetX);
                            Canvas.SetTop(label, anchorPoint.Y - 18.0);

                          //  BlocksCanvas.Children.Add(label);
                            connection.VisualLabel = label;
                        }
                    }
                    this.connectionLines.Add(connection);
                    loadedConnections++;
                }
                HighlightAvailableCells();
                BuildSyntaxTree();

                ShowNotification($"Блок-схема успешно загружена из: {file.Name}");
            }
            catch (Exception ex)
            {
                ShowNotification($"Не удалось загрузить блок-схему:\n{ex.Message}\n\nПроверьте формат файла.");
            }
        }

        private void FlowchartCanvas_DragOver(object sender, DragEventArgs e)
        {
            if (isDraggingFromPanel && draggedBlockTemplate != null)
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
                e.DragUIOverride.Caption = $"Разместить {draggedBlockTemplate.Name}";

                var position = e.GetPosition(FlowchartCanvas);
                ShowDragPreview(position);

                e.Handled = true;
            }
        }

        private void FlowchartCanvas_Drop(object sender, DragEventArgs e)
        {
            if (isDraggingFromPanel && draggedBlockTemplate != null)
            {
                var position = e.GetPosition(FlowchartCanvas);
                double snappedX = SnapToGrid(position.X - 50);
                double snappedY = SnapToGrid(position.Y - 30);
                CreateBlockOnCanvasAtPosition(draggedBlockTemplate, snappedX, snappedY);
                RemoveDragPreview();

                isDraggingFromPanel = false;
                draggedBlockTemplate = null;
                e.Handled = true;
            }
        }

        private void FlowchartCanvas_DragLeave(object sender, DragEventArgs e)
        {
            RemoveDragPreview();
        }

        private void ShowDragPreview(Point position)
        {
            if (dragPreviewBorder == null && draggedBlockTemplate != null)
            {
                var previewBlock = new BlockItem
                {
                    Name = draggedBlockTemplate.Name,
                    Icon = draggedBlockTemplate.Icon,
                    Type = draggedBlockTemplate.Type
                };

                dragPreviewBorder = DrawBlock.GetBlock(previewBlock);
                dragPreviewBorder.Opacity = 0.5;
                FlowchartCanvas.Children.Add(dragPreviewBorder);

                foreach (var block in listofblocks)
                {
                    if (block.Type == previewBlock.Type &&
                        (previewBlock.Type == BlockType.Start || previewBlock.Type == BlockType.End))
                    {
                        if (this.Content?.XamlRoot == null) return;
                        RemoveDragPreview();
                        return;
                    }
                }
            }


            if (dragPreviewBorder != null)
            {
                double snappedX = SnapToGrid(position.X - 50);
                double snappedY = SnapToGrid(position.Y - 30);

                Canvas.SetLeft(dragPreviewBorder, snappedX);
                Canvas.SetTop(dragPreviewBorder, snappedY);
            }
        }

        private void RemoveDragPreview()
        {
            if (dragPreviewBorder != null)
            {
                FlowchartCanvas.Children.Remove(dragPreviewBorder);
                dragPreviewBorder = null;
            }
        }

        private void CreateBlockOnCanvasAtPosition(BlockItem templateBlock, double x, double y)
        {
            SaveState();

            if (templateBlock.Type == BlockType.While)
            {
                CreateWhileLoopStructure(x, y, templateBlock.Shot, templateBlock.Docs);
                return;
            }
            if (templateBlock.Type == BlockType.DoWhile)
            {
                CreateDoWhileLoopStructure(x, y, templateBlock.Shot, templateBlock.Docs);
                return;
            }

            if (templateBlock.Type == BlockType.For)
            {
                CreateForLoopStructure(x, y, templateBlock.Shot, templateBlock.Docs);
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
                        CreateBeginEndStructure(x, y); 
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
                Docs = templateBlock.Docs,
                Shot = templateBlock.Shot,
                Type = templateBlock.Type,
                Id = Guid.NewGuid()
            };


            int col = (int)Math.Round(x / (double)SettingsWindow.AppSettings.GridStep);
            int row = (int)Math.Round(y / (double)SettingsWindow.AppSettings.GridStep);

            row = Math.Max(0, Math.Min(GRID_ROWS - 1, row));
            col = Math.Max(0, Math.Min(GRID_COLUMNS - 1, col));

            GridNode targetNode = virtualGrid[row, col];
            if (!targetNode.IsAvailable)
            {
                targetNode = FindNearestFreeNode(row, col);
                if (targetNode == null)
                {
                    ShowNotification("Нет места: Свободные ячейки для блоков отсутствуют.");
                    blockCounter--;
                    return;
                }
            }

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

            targetNode.OccupiedBy = newBlock;
            newBlock.CanvasLeft = targetNode.Column * SettingsWindow.AppSettings.GridStep;
            newBlock.CanvasTop = targetNode.Row * SettingsWindow.AppSettings.GridStep;
            newBlock.GridPosition = targetNode;
            Rect blockRect = new Rect(newBlock.CanvasLeft, newBlock.CanvasTop, 100, 60);
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

                CreateManualConnection(lineToInsertInto.FromBlock, newBlock, lineToInsertInto.Type);
                ConnectionType outType = ConnectionType.Normal;

                if (newBlock.Type == BlockType.Decision)
                {
                    outType = ConnectionType.TrueBranch;
                }
                else if (newBlock.Type == BlockType.While || newBlock.Type == BlockType.For || newBlock.Type == BlockType.DoWhile)
                {
                    outType = ConnectionType.FalseBranch;
                }

                CreateManualConnection(newBlock, lineToInsertInto.ToBlock, outType);
            }

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
            UpdateConnectionLines(newBlock);
            UpdateStatusBar();
        }

        private void ScrollToBlock(BlockItem blockItem)
        {
            var blockBorder = BlocksCanvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag == blockItem);

            if (blockBorder != null && MainScrollViewer != null)
            {
                var blockX = Canvas.GetLeft(blockBorder);
                var blockY = Canvas.GetTop(blockBorder);
                var blockWidth = blockBorder.ActualWidth > 0 ? blockBorder.ActualWidth : 100;
                var blockHeight = blockBorder.ActualHeight > 0 ? blockBorder.ActualHeight : 60;

                var viewportWidth = MainScrollViewer.ActualWidth;
                var viewportHeight = MainScrollViewer.ActualHeight;

                var targetOffsetX = (blockX + blockWidth / 2) - (viewportWidth / 2);
                var targetOffsetY = (blockY + blockHeight / 2) - (viewportHeight / 2);

                targetOffsetX = Math.Max(0, Math.Min(targetOffsetX, BlocksCanvas.Width - viewportWidth));
                targetOffsetY = Math.Max(0, Math.Min(targetOffsetY, BlocksCanvas.Height - viewportHeight));

                MainScrollViewer.ChangeView(targetOffsetX, targetOffsetY, null);
            }
        }
        private void UpdateBlockVariableState(BlockItem blockItem, Dictionary<string, double> variables)
        {
            if (!blockVariablePanels.TryGetValue(blockItem, out Border previewBorder))
            {
                previewBorder = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(180, 119, 119, 119)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(5),
                    Tag = blockItem
                };
                var stackPanel = new StackPanel();
                previewBorder.Child = stackPanel;
                BlocksCanvas.Children.Add(previewBorder);
                blockVariablePanels.Add(blockItem, previewBorder);
            }
            var stackPanelToUpdate = previewBorder.Child as StackPanel;
            stackPanelToUpdate.Children.Clear();
            foreach (var kvp in variables)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{kvp.Key}: {kvp.Value:G5}",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                stackPanelToUpdate.Children.Add(textBlock);
            }

            var blockBorder = BlocksCanvas.Children.OfType<Border>()
                .FirstOrDefault(b => b.Tag == blockItem);

            if (blockBorder != null)
            {
                double blockLeft = Canvas.GetLeft(blockBorder);
                double blockTop = Canvas.GetTop(blockBorder);
                double blockWidth = blockBorder.ActualWidth > 0 ? blockBorder.ActualWidth : 100;

                double previewLeft = blockLeft + blockWidth + 10;
                double previewTop = blockTop;

                Canvas.SetLeft(previewBorder, previewLeft);
                Canvas.SetTop(previewBorder, previewTop);
                previewBorder.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Удаляет все панели превью
        /// </summary>
        private void ClearVariablePreviewPanels()
        {
            var panelsToRemove = blockVariablePanels.Values.ToList();
            foreach (var panel in panelsToRemove)
                if (BlocksCanvas.Children.Contains(panel))
                    BlocksCanvas.Children.Remove(panel);
            blockVariablePanels.Clear();
        }

        private void RemoveConnection(ConnectionLine connection)
        {
            if (connectionLines.Contains(connection))
            {
                if (connection.VisualPath != null)
                    FlowchartCanvas.Children.Remove(connection.VisualPath);

                if (connection.ArrowHead != null)
                    FlowchartCanvas.Children.Remove(connection.ArrowHead);
                if (connection.VisualLabel != null)
                {
                    BlocksCanvas.Children.Remove(connection.VisualLabel);
                    connection.VisualLabel = null;
                }
                connectionLines.Remove(connection);
            }
        }
    }
}
