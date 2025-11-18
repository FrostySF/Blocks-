using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        public void CreateBeginEndStructureInViewport()
        {
            SaveState();

            GridNode startNode = FindFirstFreeGridNodeInViewport();
            if (startNode == null)
            {
                ShowNotification("Свободные ячейки для блоков отсутствуют.");
                return;
            }

            int endCol = startNode.Column;
            int endRow = startNode.Row + 2;

            if (endRow >= GRID_ROWS)
            {
                ShowNotification("Недостаточно места для структуры Begin/End в текущей области.");
                return;
            }

            GridNode endNode = virtualGrid[endRow, endCol];
            blockCounter++;
            var _startBlock = new BlockItem
            {
                Type = BlockType.Start,
                Name = $"Начало {blockCounter}",
                Description = "Начало выполнения программы",
                Id = Guid.NewGuid(),
                CanvasLeft = startNode.Column * GRID_STEP,
                CanvasTop = startNode.Row * GRID_STEP,
                GridPosition = startNode
            };

            startNode.OccupiedBy = _startBlock;
            listofblocks.Add(_startBlock);
            startBlock = _startBlock;
            Border startBorder = DrawBlock.GetBlock(_startBlock);
            startBorder.Tag = _startBlock;
            startBorder.PointerPressed += BlockControl_PointerPressed;
            startBorder.PointerReleased += BlockControl_PointerReleased;
            startBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(startBorder);
            InitializeBlockContextMenu(startBorder);

            Canvas.SetLeft(startBorder, _startBlock.CanvasLeft);
            Canvas.SetTop(startBorder, _startBlock.CanvasTop);
            BlocksCanvas.Children.Add(startBorder);

            var _endBlock = new BlockItem
            {
                Type = BlockType.End,
                Name = $"Конец {blockCounter}",
                Description = "Конец выполнения программы",
                Id = Guid.NewGuid(),
                CanvasLeft = endNode.Column * GRID_STEP,
                CanvasTop = endNode.Row * GRID_STEP,
                GridPosition = endNode
            };
            endNode.OccupiedBy = _endBlock;
            listofblocks.Add(_endBlock);
            endBlock = _endBlock;
            Border endBorder = DrawBlock.GetBlock(_endBlock);
            endBorder.Tag = _endBlock;
            endBorder.PointerPressed += BlockControl_PointerPressed;
            endBorder.PointerReleased += BlockControl_PointerReleased;
            endBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(endBorder);
            InitializeBlockContextMenu(endBorder);

            Canvas.SetLeft(endBorder, _endBlock.CanvasLeft);
            Canvas.SetTop(endBorder, _endBlock.CanvasTop);
            BlocksCanvas.Children.Add(endBorder);
            CreateManualConnection(_startBlock, _endBlock, ConnectionType.Normal);
            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification($"Структура Begin/End создана:\n- Начало: {_startBlock.Name}\n- Конец: {_endBlock.Name}");
        }


        public void CreateBeginEndStructure(double startX, double startY)
        {
            SaveState();

            int startCol = (int)Math.Round(startX / (double)GRID_STEP);
            int startRow = (int)Math.Round(startY / (double)GRID_STEP);
            startRow = Math.Max(0, Math.Min(GRID_ROWS - 1, startRow));
            startCol = Math.Max(0, Math.Min(GRID_COLUMNS - 1, startCol));

            GridNode startNode = virtualGrid[startRow, startCol];

            if (!startNode.IsAvailable)
            {
                startNode = FindNearestFreeNode(startRow, startCol);
                if (startNode == null)
                {
                    ShowNotification("Нет места для блока START.");
                    return;
                }
            }

            int endCol = startNode.Column;
            int endRow = startNode.Row + 2;

            if (endRow >= GRID_ROWS)
            {
                ShowNotification("Недостаточно места для структуры Begin/End.");
                return;
            }

            GridNode endNode = virtualGrid[endRow, endCol];

            blockCounter++;
            var _startBlock = new BlockItem
            {
                Type = BlockType.Start,
                Name = $"Начало {blockCounter}",
                Description = "Начало выполнения программы",
                Id = Guid.NewGuid(),
                CanvasLeft = startNode.Column * GRID_STEP,
                CanvasTop = startNode.Row * GRID_STEP,
                GridPosition = startNode
            };


            startNode.OccupiedBy = _startBlock;
            listofblocks.Add(_startBlock);
            startBlock = _startBlock;

            Border startBorder = DrawBlock.GetBlock(_startBlock);
            startBorder.Tag = _startBlock;
            startBorder.PointerPressed += BlockControl_PointerPressed;
            startBorder.PointerReleased += BlockControl_PointerReleased;
            startBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(startBorder);
            InitializeBlockContextMenu(startBorder);

            Canvas.SetLeft(startBorder, _startBlock.CanvasLeft);
            Canvas.SetTop(startBorder, _startBlock.CanvasTop);
            BlocksCanvas.Children.Add(startBorder);

            var _endBlock = new BlockItem
            {
                Type = BlockType.End,
                Name = $"Конец {blockCounter}",
                Description = "Конец выполнения программы",
                Id = Guid.NewGuid(),
                CanvasLeft = endNode.Column * GRID_STEP,
                CanvasTop = endNode.Row * GRID_STEP,
                GridPosition = endNode
            };

            endNode.OccupiedBy = _endBlock;
            listofblocks.Add(_endBlock);
            endBlock = _endBlock;
            Border endBorder = DrawBlock.GetBlock(_endBlock);
            endBorder.Tag = _endBlock;
            endBorder.PointerPressed += BlockControl_PointerPressed;
            endBorder.PointerReleased += BlockControl_PointerReleased;
            endBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(endBorder);
            InitializeBlockContextMenu(endBorder);

            Canvas.SetLeft(endBorder, _endBlock.CanvasLeft);
            Canvas.SetTop(endBorder, _endBlock.CanvasTop);
            BlocksCanvas.Children.Add(endBorder);
            CreateManualConnection(_startBlock, _endBlock, ConnectionType.Normal);
            HighlightAvailableCells();
            BuildSyntaxTree();
            ShowNotification($"Структура Begin/End создана по координатам:\n- Начало: {_startBlock.Name}\n- Конец: {_endBlock.Name}");
        }
    }
}
