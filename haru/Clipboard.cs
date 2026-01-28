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
using Windows.Foundation;
using Windows.UI;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        private void InitializeClipboardAndUndo()
        {
            SaveState();

            if (this.Content is FrameworkElement root)
            {
                root.KeyDown += CoreWindow_KeyDown_Enhanced;
            }

            selectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.DeepSkyBlue),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(40, 0, 191, 255)),
                StrokeDashArray = new DoubleCollection { 5, 3 },
                Visibility = Visibility.Collapsed,
                IsHitTestVisible = false
            };
            FlowchartCanvas.Children.Add(selectionRectangle);

            BlocksCanvas.PointerPressed += BlocksCanvas_PointerPressed;
            BlocksCanvas.PointerMoved += BlocksCanvas_PointerMoved;
            BlocksCanvas.PointerReleased += BlocksCanvas_PointerReleased;
        }

        private void BlocksCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var pointerPoint = e.GetCurrentPoint(BlocksCanvas);

            if (e.OriginalSource == BlocksCanvas || e.OriginalSource == sender)
            {
                if (pointerPoint.Properties.IsLeftButtonPressed && !IsCtrlPressed() && !isSpaceBarPressed)
                {
                    isMultiSelecting = true;
                    selectionStartPoint = pointerPoint.Position;

                    ClearSelection();

                    selectionRectangle.Visibility = Visibility.Visible;
                    Canvas.SetLeft(selectionRectangle, selectionStartPoint.X);
                    Canvas.SetTop(selectionRectangle, selectionStartPoint.Y);
                    selectionRectangle.Width = 0;
                    selectionRectangle.Height = 0;

                    BlocksCanvas.CapturePointer(e.Pointer);
                    e.Handled = true;
                }
            }
        }

        private void BlocksCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (isMultiSelecting)
            {
                var currentPoint = e.GetCurrentPoint(BlocksCanvas).Position;
                UpdateSelectionRectangle(currentPoint);
                e.Handled = true;
            }
        }

        private void BlocksCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (isMultiSelecting)
            {
                isMultiSelecting = false;
                selectionRectangle.Visibility = Visibility.Collapsed;
                BlocksCanvas.ReleasePointerCapture(e.Pointer);

                if (selectedBlocks.Count > 0)
                    ShowNotification($"Выделено блоков: {selectedBlocks.Count}");

                e.Handled = true;
            }
        }

        private void CoreWindow_KeyDown_Enhanced(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = IsCtrlPressed();
            var shift = IsShiftPressed();

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceBarPressed = true;
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Windows.System.VirtualKey.C)
            {
                CopySelectedBlocks();
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Windows.System.VirtualKey.X)
            {
                CutSelectedBlocks();
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Windows.System.VirtualKey.V)
            {
                PasteBlocks();
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Windows.System.VirtualKey.Z)
            {
                Undo();
                e.Handled = true;
                return;
            }
            if ((ctrl && e.Key == Windows.System.VirtualKey.Y) ||
                (ctrl && shift && e.Key == Windows.System.VirtualKey.Z))
            {
                Redo();
                e.Handled = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Delete)
            {
                DeleteSelectedBlocks();
                e.Handled = true;
                return;
            }
            if (ctrl && e.Key == Windows.System.VirtualKey.A)
            {
                SelectAllBlocks();
                e.Handled = true;
                return;
            }
            if (e.Key == Windows.System.VirtualKey.Escape)
            {
                ClearSelection();
                e.Handled = true;
                return;
            }
        }

        private bool IsCtrlPressed()
        {
            return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }

        private bool IsShiftPressed()
        {
            return Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        }

        private void SaveState()
        {
            var state = new FlowchartState
            {
                Blocks = listofblocks.ToList(),
                Connections = connectionLines.ToList(),
                BlockCounter = blockCounter
            };

            undoStack.Push(state.Clone());

            while (undoStack.Count > SettingsWindow.AppSettings.MaxUndoSteps)
            {
                var tempStack = new Stack<FlowchartState>();
                for (int i = 0; i < SettingsWindow.AppSettings.MaxUndoSteps; i++)
                    tempStack.Push(undoStack.Pop());
                undoStack.Clear();
                while (tempStack.Count > 0)
                    undoStack.Push(tempStack.Pop());
            }
            UpdateStatusBar();
            redoStack.Clear();
        }

        private void Undo()
        {
            if (undoStack.Count <= 1)
            {
                ShowNotification("Нечего отменять");
                return;
            }

            var currentState = new FlowchartState
            {
                Blocks = listofblocks.ToList(),
                Connections = connectionLines.ToList(),
                BlockCounter = blockCounter
            };
            redoStack.Push(currentState.Clone());

            undoStack.Pop();
            var previousState = undoStack.Peek();
            RestoreState(previousState);

            ShowNotification("Отмена выполнена");
        }

        private void Redo()
        {
            if (redoStack.Count == 0)
            {
                ShowNotification("Нечего повторять");
                return;
            }

            var nextState = redoStack.Pop();

            var currentState = new FlowchartState
            {
                Blocks = listofblocks.ToList(),
                Connections = connectionLines.ToList(),
                BlockCounter = blockCounter
            };
            undoStack.Push(currentState.Clone());

            RestoreState(nextState);
            ShowNotification("Повтор выполнен");
        }

        private void RestoreState(FlowchartState state)
        {
            BlocksCanvas.Children.Clear();
            var linesToRemove = FlowchartCanvas.Children.OfType<Polyline>().ToList();
            foreach (var line in linesToRemove)
                FlowchartCanvas.Children.Remove(line);
            var arrowsToRemove = FlowchartCanvas.Children.OfType<Polygon>().ToList();
            foreach (var arrow in arrowsToRemove)
                FlowchartCanvas.Children.Remove(arrow);

            listofblocks.Clear();
            connectionLines.Clear();
            InitializeVirtualGrid();

            foreach (var block in state.Blocks)
            {
                listofblocks.Add(block);

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
                border.PointerPressed += BlockControl_PP;
                border.PointerReleased += BlockControl_PointerReleased;
                border.DoubleTapped += BlockControl_DoubleTapped;
                AttachAnchorHandlers(border);
                InitializeBlockContextMenu(border);

                Canvas.SetLeft(border, block.CanvasLeft);
                Canvas.SetTop(border, block.CanvasTop);
                BlocksCanvas.Children.Add(border);
            }

            foreach (var connection in state.Connections)
            {
                var fromBlock = listofblocks.FirstOrDefault(b => b.Id == connection.FromBlock.Id);
                var toBlock = listofblocks.FirstOrDefault(b => b.Id == connection.ToBlock.Id);

                if (fromBlock != null && toBlock != null)
                    CreateManualConnection(fromBlock, toBlock, connection.Type);
            }

            blockCounter = state.BlockCounter;
            HighlightAvailableCells();
            BuildSyntaxTree();
        }

        private void CopySelectedBlocks()
        {
            if (selectedBlocks.Count == 0)
            {
                ShowNotification("Нет выделенных блоков для копирования");
                return;
            }

            clipboard.Clear();
            clipboardConnections.Clear();

            foreach (var block in selectedBlocks)
            {
                var copy = new BlockItem
                {
                    Name = block.Name,
                    Icon = block.Icon,
                    Description = block.Description,
                    Type = block.Type,
                    Code = block.Code,
                    Id = Guid.NewGuid()
                };
                clipboard.Add(copy);
            }

            foreach (var conn in connectionLines)
                if (selectedBlocks.Contains(conn.FromBlock) && selectedBlocks.Contains(conn.ToBlock))
                    clipboardConnections.Add(new ConnectionLine
                    {
                        FromBlock = conn.FromBlock,
                        ToBlock = conn.ToBlock,
                        Type = conn.Type
                    });

            ShowNotification($"Скопировано блоков: {clipboard.Count}");
        }

        private void CutSelectedBlocks()
        {
            if (selectedBlocks.Count == 0)
            {
                ShowNotification("Нет выделенных блоков для вырезания");
                return;
            }

            SaveState();
            CopySelectedBlocks();
            DeleteSelectedBlocks();
            ShowNotification($"Вырезано блоков: {clipboard.Count}");
        }

        private void PasteBlocks()
        {
            if (clipboard.Count == 0)
            {
                ShowNotification("Буфер обмена пуст");
                return;
            }

            bool startExists = listofblocks.Any(b => b.Type == BlockType.Start);
            bool endExists = listofblocks.Any(b => b.Type == BlockType.End);

            bool clipboardHasStart = clipboard.Any(b => b.Type == BlockType.Start);
            bool clipboardHasEnd = clipboard.Any(b => b.Type == BlockType.End);

            if ((startExists && clipboardHasStart) || (endExists && clipboardHasEnd))
            {
                string conflict = (startExists && clipboardHasStart) ? "Начало" : "Конец";
                ShowNotification($"Вставка невозможна: блок '{conflict}' уже существует на холсте.");
                return;
            }

            SaveState();
            ClearSelection();

            var idMapping = new Dictionary<Guid, BlockItem>();

            foreach (var clipboardBlock in clipboard)
            {
                blockCounter++;
                var newBlock = new BlockItem
                {
                    Name = $"{clipboardBlock.Name} (копия)",
                    Icon = clipboardBlock.Icon,
                    Description = clipboardBlock.Description,
                    Type = clipboardBlock.Type,
                    Code = clipboardBlock.Code,
                    Id = Guid.NewGuid()
                };

                idMapping[clipboardBlock.Id] = newBlock;

                GridNode targetNode = FindFirstFreeGridNodeInViewport();
                if (targetNode == null)
                {
                    ShowNotification("Нет свободного места для вставки");
                    return;
                }

                targetNode.OccupiedBy = newBlock;
                newBlock.CanvasLeft = targetNode.Column * SettingsWindow.AppSettings.GridStep;
                newBlock.CanvasTop = targetNode.Row * SettingsWindow.AppSettings.GridStep;
                newBlock.GridPosition = targetNode;

                Border border = DrawBlock.GetBlock(newBlock);
                border.Tag = newBlock;
                border.PointerPressed += BlockControl_PP;
                border.PointerReleased += BlockControl_PointerReleased;
                border.DoubleTapped += BlockControl_DoubleTapped;
                AttachAnchorHandlers(border);
                InitializeBlockContextMenu(border);

                Canvas.SetLeft(border, newBlock.CanvasLeft);
                Canvas.SetTop(border, newBlock.CanvasTop);
                BlocksCanvas.Children.Add(border);

                listofblocks.Add(newBlock);
                selectedBlocks.Add(newBlock);
                HighlightBlock(border, true);
            }

            foreach (var conn in clipboardConnections)
            {
                if (idMapping.ContainsKey(conn.FromBlock.Id) && idMapping.ContainsKey(conn.ToBlock.Id))
                {
                    CreateManualConnection(
                        idMapping[conn.FromBlock.Id],
                        idMapping[conn.ToBlock.Id],
                        conn.Type
                    );
                }
            }

            HighlightAvailableCells();
            ShowNotification($"Вставлено блоков: {clipboard.Count}");
        }

        private void BlockControl_PP(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is BlockItem block)
            {
                var ctrl = IsCtrlPressed();

                var originalSource = e.OriginalSource;

                DependencyObject parent = originalSource as DependencyObject;
                bool isAnchorClick = false;

                while (parent != null && parent != border)
                {
                    if (parent is Ellipse ellipse && ellipse.Name == "AnchorEllipse")
                    {
                        isAnchorClick = true;
                        break;
                    }
                    if (parent is Border hitBox && hitBox.Tag is ValueTuple<BlockItem, ConnectionType>)
                    {
                        isAnchorClick = true;
                        break;
                    }
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (isAnchorClick)
                    return;

                if (ctrl)
                {
                    if (selectedBlocks.Contains(block))
                    {
                        selectedBlocks.Remove(block);
                        HighlightBlock(border, false);
                    }
                    else
                    {
                        selectedBlocks.Add(block);
                        HighlightBlock(border, true);
                    }
                    e.Handled = true;
                    return;
                }

                if (!selectedBlocks.Contains(block))
                {
                    ClearSelection();
                    selectedBlocks.Add(block);
                    HighlightBlock(border, true);
                }

                selectedBlock = block;
                clickedBlock = block;
                isDraggingSelection = true;
                lastMousePosition = e.GetCurrentPoint(FlowchartCanvas).Position;
                border.CapturePointer(e.Pointer);
                foreach (var selectedBlock in selectedBlocks)
                {
                    if (selectedBlock.GridPosition != null)
                    {
                        virtualGrid[selectedBlock.GridPosition.Row, selectedBlock.GridPosition.Column].OccupiedBy = null;
                        selectedBlock.GridPosition = null;
                    }
                }

                HighlightAvailableCells();
                e.Handled = true;
            }
        }

        private void HighlightBlock(Border border, bool highlight)
        {
            if (border.Child is Grid grid)
                foreach (var child in grid.Children)
                    if (child is Shape shape && !(child is Ellipse && ((Ellipse)child).Name == "AnchorEllipse"))
                    {
                        shape.Stroke = new SolidColorBrush(highlight ? Colors.Yellow : Colors.White);
                        shape.StrokeThickness = highlight ? 3 : 2;
                    }
        }

        private void ClearSelection()
        {
            foreach (var block in selectedBlocks.ToList())
            {
                var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                if (border != null)
                {
                    HighlightBlock(border, false);
                }
            }
            selectedBlocks.Clear();

            if (selectionRectangle != null)
                selectionRectangle.Visibility = Visibility.Collapsed;
        }

        private void SelectAllBlocks()
        {
            ClearSelection();
            foreach (var block in listofblocks)
            {
                selectedBlocks.Add(block);
                var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                if (border != null)
                    HighlightBlock(border, true);
            }
            ShowNotification($"Выделено блоков: {selectedBlocks.Count}");
        }

        private void DeleteSelectedBlocks()
        {
            if (selectedBlocks.Count == 0)
            {
                ShowNotification("Нет выделенных блоков для удаления");
                return;
            }

            SaveState();

            foreach (var block in selectedBlocks.ToList())
            {
                var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                if (border != null)
                    DeleteBlock(border);
            }

            selectedBlocks.Clear();
            ShowNotification("Выделенные блоки удалены");
        }

        private void UpdateSelectionRectangle(Point currentPoint)
        {
            if (!isMultiSelecting) return;

            double left = Math.Min(selectionStartPoint.X, currentPoint.X);
            double top = Math.Min(selectionStartPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - selectionStartPoint.X);
            double height = Math.Abs(currentPoint.Y - selectionStartPoint.Y);

            Canvas.SetLeft(selectionRectangle, left);
            Canvas.SetTop(selectionRectangle, top);
            selectionRectangle.Width = width;
            selectionRectangle.Height = height;

            Rect selectionRect = new Rect(left, top, width, height);

            foreach (var block in listofblocks)
            {
                Rect blockRect = new Rect(block.CanvasLeft, block.CanvasTop, 100, 60);

                var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                if (border != null)
                {
                    bool intersects = DoRectsIntersect(selectionRect, blockRect);

                    if (intersects && !selectedBlocks.Contains(block))
                    {
                        selectedBlocks.Add(block);
                        HighlightBlock(border, true);
                    }
                    else if (!intersects && selectedBlocks.Contains(block))
                    {
                        selectedBlocks.Remove(block);
                        HighlightBlock(border, false);
                    }
                }
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e) => CopySelectedBlocks();
        private void Cut_Click(object sender, RoutedEventArgs e) => CutSelectedBlocks();
        private void Paste_Click(object sender, RoutedEventArgs e) => PasteBlocks();
        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();
    }
}