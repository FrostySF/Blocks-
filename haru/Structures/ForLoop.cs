using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        public void CreateForLoopStructureInViewport()
        {
            SaveState();

            GridNode loopNode = FindFirstFreeGridNodeInViewport();
            if (loopNode == null)
            {
                ShowNotification("Свободные ячейки отсутствуют.");
                return;
            }

            int connectorCol = loopNode.Column + 2;
            int connectorRow = loopNode.Row;

            if (connectorCol >= GRID_COLUMNS)
            {
                ShowNotification("Недостаточно места для цикла FOR.");
                return;
            }

            GridNode connectorNode = virtualGrid[connectorRow, connectorCol];

            if (!connectorNode.IsAvailable)
            {
                connectorNode = FindNearestFreeNode(connectorRow, connectorCol);
                if (connectorNode == null)
                {
                    ShowNotification("Нет места для тела цикла FOR.");
                    return;
                }
            }

            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.For,
                Name = $"Подготовка {blockCounter}",
                Description = "Цикл 'FOR'",
                Id = Guid.NewGuid(),
                CanvasLeft = loopNode.Column * GRID_STEP,
                CanvasTop = loopNode.Row * GRID_STEP,
                GridPosition = loopNode
            };

            loopNode.OccupiedBy = loopBlock;
            listofblocks.Add(loopBlock);

            Border loopBorder = DrawBlock.GetBlock(loopBlock);
            loopBorder.Tag = loopBlock;
            loopBorder.PointerPressed += BlockControl_PointerPressed;
            loopBorder.PointerReleased += BlockControl_PointerReleased;
            loopBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(loopBorder);
            InitializeBlockContextMenu(loopBorder);

            Canvas.SetLeft(loopBorder, loopBlock.CanvasLeft);
            Canvas.SetTop(loopBorder, loopBlock.CanvasTop);
            BlocksCanvas.Children.Add(loopBorder);

            var connectorBlock = new BlockItem
            {
                Type = BlockType.LoopConnector,
                Name = $"CONN{blockCounter}",
                Description = "Тело цикла FOR",
                Code = "",
                Id = Guid.NewGuid(),
                CanvasLeft = connectorNode.Column * GRID_STEP,
                CanvasTop = connectorNode.Row * GRID_STEP,
                GridPosition = connectorNode
            };

            connectorNode.OccupiedBy = connectorBlock;
            listofblocks.Add(connectorBlock);

            Border connectorBorder = DrawBlock.GetBlock(connectorBlock);
            connectorBorder.Tag = connectorBlock;
            connectorBorder.PointerPressed += BlockControl_PointerPressed;
            connectorBorder.PointerReleased += BlockControl_PointerReleased;
            connectorBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(connectorBorder);
            InitializeBlockContextMenu(connectorBorder);

            Canvas.SetLeft(connectorBorder, connectorBlock.CanvasLeft);
            Canvas.SetTop(connectorBorder, connectorBlock.CanvasTop);
            BlocksCanvas.Children.Add(connectorBorder);

            // FOR(TrueBranch) → тело
            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.TrueBranch);

            // тело(Normal) → FOR (инкремент)
            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);

            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification(
                $"Структура FOR создана в области видимости:\n" +
                $"- Условие: {loopBlock.Name}\n" +
                $"- Тело: {connectorBlock.Name}"
            );
        }

        public void CreateForLoopStructure(double startX, double startY)
        {
            SaveState();

            // 1. Вычисляем координаты сетки для блока условия (ромб FOR)
            int loopCol = (int)Math.Round(startX / (double)GRID_STEP);
            int loopRow = (int)Math.Round(startY / (double)GRID_STEP);

            loopRow = Math.Max(0, Math.Min(GRID_ROWS - 1, loopRow));
            loopCol = Math.Max(0, Math.Min(GRID_COLUMNS - 1, loopCol));

            GridNode loopNode = virtualGrid[loopRow, loopCol];

            if (!loopNode.IsAvailable)
            {
                loopNode = FindNearestFreeNode(loopRow, loopCol);
                if (loopNode == null)
                {
                    ShowNotification("Нет места для блока цикла FOR.");
                    return;
                }
            }

            // 2. Ячейка для тела — справа на 2 клетки
            int connectorCol = loopNode.Column + 2;
            int connectorRow = loopNode.Row;

            if (connectorCol >= GRID_COLUMNS)
            {
                ShowNotification("Недостаточно места для структуры цикла FOR.");
                return;
            }

            GridNode connectorNode = virtualGrid[connectorRow, connectorCol];

            if (!connectorNode.IsAvailable)
            {
                connectorNode = FindNearestFreeNode(connectorRow, connectorCol);
                if (connectorNode == null)
                {
                    ShowNotification("Нет места для тела цикла FOR.");
                    return;
                }
            }

            // 3. Создаем блок FOR
            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.For,
                Name = $"Подготовка {blockCounter}",
                Description = "Цикл 'FOR'",
                Id = Guid.NewGuid(),
                CanvasLeft = loopNode.Column * GRID_STEP,
                CanvasTop = loopNode.Row * GRID_STEP,
                GridPosition = loopNode
            };

            loopNode.OccupiedBy = loopBlock;
            listofblocks.Add(loopBlock);

            Border loopBorder = DrawBlock.GetBlock(loopBlock);
            loopBorder.Tag = loopBlock;
            loopBorder.PointerPressed += BlockControl_PointerPressed;
            loopBorder.PointerReleased += BlockControl_PointerReleased;
            loopBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(loopBorder);
            InitializeBlockContextMenu(loopBorder);

            Canvas.SetLeft(loopBorder, loopBlock.CanvasLeft);
            Canvas.SetTop(loopBorder, loopBlock.CanvasTop);
            BlocksCanvas.Children.Add(loopBorder);

            // 4. Создаем тело цикла (connector)
            var connectorBlock = new BlockItem
            {
                Type = BlockType.LoopConnector,
                Name = $"CONN{blockCounter}",
                Description = "Тело цикла FOR",
                Code = "",
                Id = Guid.NewGuid(),
                CanvasLeft = connectorNode.Column * GRID_STEP,
                CanvasTop = connectorNode.Row * GRID_STEP,
                GridPosition = connectorNode
            };

            connectorNode.OccupiedBy = connectorBlock;
            listofblocks.Add(connectorBlock);

            Border connectorBorder = DrawBlock.GetBlock(connectorBlock);
            connectorBorder.Tag = connectorBlock;
            connectorBorder.PointerPressed += BlockControl_PointerPressed;
            connectorBorder.PointerReleased += BlockControl_PointerReleased;
            connectorBorder.DoubleTapped += BlockControl_DoubleTapped;
            AttachAnchorHandlers(connectorBorder);
            InitializeBlockContextMenu(connectorBorder);

            Canvas.SetLeft(connectorBorder, connectorBlock.CanvasLeft);
            Canvas.SetTop(connectorBorder, connectorBlock.CanvasTop);
            BlocksCanvas.Children.Add(connectorBorder);

            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.TrueBranch);

            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);

            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification(
                $"Структура цикла FOR создана:\n" +
                $"- Условие: {loopBlock.Name}\n" +
                $"- Тело: {connectorBlock.Name}\n\n" +
                $"True → тело\nFalse → выход\nТело → FOR (инкремент)"
            );
        }

    }
}