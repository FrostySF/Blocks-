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
        /// <summary>
        /// Создает структуру цикла Do-While в указанных координатах
        /// </summary>
        public void CreateDoWhileLoopStructure(double startX, double startY)
        {
            SaveState();

            int loopCol = (int)Math.Round(startX / (double)SettingsWindow.AppSettings.GridStep);
            int loopRow = (int)Math.Round(startY / (double)SettingsWindow.AppSettings.GridStep);

            loopRow = Math.Max(0, Math.Min(GRID_ROWS - 1, loopRow));
            loopCol = Math.Max(0, Math.Min(GRID_COLUMNS - 1, loopCol));

            GridNode loopNode = virtualGrid[loopRow, loopCol];

            if (!loopNode.IsAvailable)
            {
                loopNode = FindNearestFreeNode(loopRow, loopCol);
                if (loopNode == null)
                {
                    ShowNotification("Нет места для блока условия цикла.");
                    return;
                }
            }

            int connectorCol = loopNode.Column;
            int connectorRow = loopNode.Row - 2;

            if (connectorCol >= GRID_COLUMNS)
            {
                ShowNotification("Недостаточно места для структуры цикла.");
                return;
            }

            GridNode connectorNode = virtualGrid[connectorRow, connectorCol];

            if (!connectorNode.IsAvailable)
            {
                connectorNode = FindNearestFreeNode(connectorRow, connectorCol);
                if (connectorNode == null)
                {
                    ShowNotification("Нет места для соединителя цикла.");
                    return;
                }
            }

            // 3. Создаем блок условия цикла (DO-WHILE - ромб)
            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.DoWhile,
                Name = $"Делай {blockCounter}",
                Description = "Цикл с постусловием 'DO-WHILE'",
                Id = Guid.NewGuid(),
                CanvasLeft = loopNode.Column * SettingsWindow.AppSettings.GridStep,
                CanvasTop = loopNode.Row * SettingsWindow.AppSettings.GridStep,
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
                Type = BlockType.DoLoopConnector,
                Name = $"",
                Description = "Тело цикла",
                Code = "",
                Id = Guid.NewGuid(),
                CanvasLeft = connectorNode.Column * SettingsWindow.AppSettings.GridStep,
                CanvasTop = connectorNode.Row * SettingsWindow.AppSettings.GridStep,
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

            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);
            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.LoopBody);

            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification($"Структура цикла DO-WHILE создана:\n- Условие: {loopBlock.Name}\n- Тело: {connectorBlock.Name}\n\nТело выполняется минимум 1 раз!");
        }

        /// <summary>
        /// Создает структуру цикла Do-While в области видимости
        /// </summary>
        public void CreateDoWhileLoopStructureInViewport()
        {
            SaveState();

            GridNode loopNode = FindFirstFreeGridNodeInViewport();
            if (loopNode == null)
            {
                ShowNotification("Свободные ячейки для блоков отсутствуют.");
                return;
            }

            int connectorCol = loopNode.Column;
            int connectorRow = loopNode.Row - 2;

            if (connectorCol >= GRID_COLUMNS)
            {
                ShowNotification("Недостаточно места для структуры цикла в текущей области.");
                return;
            }

            GridNode connectorNode = virtualGrid[connectorRow, connectorCol];

            if (!connectorNode.IsAvailable)
            {
                connectorNode = FindNearestFreeNode(connectorRow, connectorCol);
                if (connectorNode == null)
                {
                    ShowNotification("Нет места для соединителя цикла.");
                    return;
                }
            }

            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.DoWhile,
                Name = $"Делай {blockCounter}",
                Description = "Цикл с постусловием 'DO-WHILE'",
                Code = "i < 10",
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
                Type = BlockType.DoLoopConnector,
                Name = $"",
                Description = "Тело цикла",

                Id = Guid.NewGuid(),
                CanvasLeft = connectorNode.Column * SettingsWindow.AppSettings.GridStep,
                CanvasTop = connectorNode.Row * SettingsWindow.AppSettings.GridStep,
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

            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);
            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.LoopBody);

            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification($"Структура цикла DO-WHILE создана в области видимости:\n- Условие: {loopBlock.Name}\n- Тело: {connectorBlock.Name}");
        }
    }
}