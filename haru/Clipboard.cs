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
    public class FlowchartState
    {
        public List<BlockItem> Blocks { get; set; }
        public List<ConnectionLine> Connections { get; set; }
        public int BlockCounter { get; set; }

        public FlowchartState Clone()
        {
            return new FlowchartState
            {
                Blocks = Blocks.Select(b => new BlockItem
                {
                    Id = b.Id,
                    Name = b.Name,
                    Icon = b.Icon,
                    Description = b.Description,
                    Type = b.Type,
                    Code = b.Code,
                    CanvasLeft = b.CanvasLeft,
                    CanvasTop = b.CanvasTop,
                    GridPosition = b.GridPosition
                }).ToList(),
                Connections = Connections.Select(c => new ConnectionLine
                {
                    FromBlock = c.FromBlock,
                    ToBlock = c.ToBlock,
                    Type = c.Type
                }).ToList(),
                BlockCounter = BlockCounter
            };
        }
    }

    public sealed partial class MainWindow : Window
    {
        private List<BlockItem> clipboard = new List<BlockItem>();
        private List<ConnectionLine> clipboardConnections = new List<ConnectionLine>();
        private Stack<FlowchartState> undoStack = new Stack<FlowchartState>();
        private Stack<FlowchartState> redoStack = new Stack<FlowchartState>();
        private const int MAX_UNDO_STEPS = 50;

        private HashSet<BlockItem> selectedBlocks = new HashSet<BlockItem>();
        private bool isMultiSelecting = false;
        private Point selectionStartPoint;
        private Rectangle selectionRectangle;

        private void InitializeClipboardAndUndo()
        {
            SaveState();

            if (this.Content is FrameworkElement root)
            {
                root.KeyDown += CoreWindow_KeyDown_Enhanced;
            }
        }

        // Расширенный обработчик клавиш
        private void CoreWindow_KeyDown_Enhanced(object sender, KeyRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
            var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceBarPressed = true;
                e.Handled = true;
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
            if (e.Key == Windows.System.VirtualKey.Space)
            {
                isSpaceBarPressed = true;
                e.Handled = true;
            }
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

            while (undoStack.Count > MAX_UNDO_STEPS)
            {
                var tempStack = new Stack<FlowchartState>();
                for (int i = 0; i < MAX_UNDO_STEPS; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    undoStack.Push(tempStack.Pop());
                }
            }

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

                int col = (int)Math.Round(block.CanvasLeft / (double)GRID_STEP);
                int row = (int)Math.Round(block.CanvasTop / (double)GRID_STEP);
                row = Math.Max(0, Math.Min(GRID_ROWS - 1, row));
                col = Math.Max(0, Math.Min(GRID_COLUMNS - 1, col));

                virtualGrid[row, col].OccupiedBy = block;
                block.GridPosition = virtualGrid[row, col];

                Border border = DrawBlock.GetBlock(block);
                border.Tag = block;
                border.PointerPressed += BlockControl_PointerPressed_Enhanced;
                border.PointerReleased += BlockControl_PointerReleased;
                border.DoubleTapped += BlockControl_DoubleTapped;
                AttachAnchorHandlers(border);
                InitializeBlockContextMenu(border);

                Canvas.SetLeft(border, block.CanvasLeft);
                Canvas.SetTop(border, block.CanvasTop);
                BlocksCanvas.Children.Add(border);
            }

            // Восстанавливаем соединения
            foreach (var connection in state.Connections)
            {
                var fromBlock = listofblocks.FirstOrDefault(b => b.Id == connection.FromBlock.Id);
                var toBlock = listofblocks.FirstOrDefault(b => b.Id == connection.ToBlock.Id);

                if (fromBlock != null && toBlock != null)
                {
                    CreateManualConnection(fromBlock, toBlock, connection.Type);
                }
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
            {
                if (selectedBlocks.Contains(conn.FromBlock) && selectedBlocks.Contains(conn.ToBlock))
                {
                    clipboardConnections.Add(new ConnectionLine
                    {
                        FromBlock = conn.FromBlock,
                        ToBlock = conn.ToBlock,
                        Type = conn.Type
                    });
                }
            }

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

            SaveState();
            ClearSelection();

            var idMapping = new Dictionary<Guid, BlockItem>();
            double offsetX = 100;
            double offsetY = 100;

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
                newBlock.CanvasLeft = targetNode.Column * GRID_STEP;
                newBlock.CanvasTop = targetNode.Row * GRID_STEP;
                newBlock.GridPosition = targetNode;

                Border border = DrawBlock.GetBlock(newBlock);
                border.Tag = newBlock;
                border.PointerPressed += BlockControl_PointerPressed_Enhanced;
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


        private void BlockControl_PointerPressed_Enhanced(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Border border && border.Tag is BlockItem block)
            {
                var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
                    .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
                var shift = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift)
                    .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

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

        private void HighlightBlock(Border border, bool highlight)
        {
            if (border.Child is Grid grid)
            {
                foreach (var child in grid.Children)
                {
                    if (child is Shape shape)
                    {
                        shape.Stroke = new SolidColorBrush(highlight ? Colors.Yellow : Colors.White);
                        shape.StrokeThickness = highlight ? 3 : 2;
                    }
                }
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
        }

        private void SelectAllBlocks()
        {
            ClearSelection();
            foreach (var block in listofblocks)
            {
                selectedBlocks.Add(block);
                var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
                if (border != null)
                {
                    HighlightBlock(border, true);
                }
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
                {
                    DeleteBlock(border);
                }
            }

            selectedBlocks.Clear();
            ShowNotification("Выделенные блоки удалены");
        }

        private void Copy_Click(object sender, RoutedEventArgs e) => CopySelectedBlocks();
        private void Cut_Click(object sender, RoutedEventArgs e) => CutSelectedBlocks();
        private void Paste_Click(object sender, RoutedEventArgs e) => PasteBlocks();
        private void Undo_Click(object sender, RoutedEventArgs e) => Undo();
        private void Redo_Click(object sender, RoutedEventArgs e) => Redo();
    }
}