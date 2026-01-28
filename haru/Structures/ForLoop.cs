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
        public void CreateForLoopStructureInViewport(string docs, string shot)
        {
            SaveState();

            GridNode loopNode = FindFirstFreeGridNodeInViewport();
            if (loopNode == null)
            {
                ShowNotification("Свободные ячейки отсутствуют.");
                return;
            }

            int connectorCol = loopNode.Column;
            int connectorRow = loopNode.Row + 2;

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
                Name = $"Подготовка",
                Description = "Цикл 'FOR'",
                Id = Guid.NewGuid(),
                Docs = docs,
                Shot = shot,
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
                Type = BlockType.LoopConnector,
                Name = $"",
                Description = "Тело цикла FOR",
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

            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.TrueBranch);
            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);

            HighlightAvailableCells();
            BuildSyntaxTree();
        }

        public void CreateForLoopStructure(double startX, double startY, string shot, string docs)
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
                    ShowNotification("Нет места для блока цикла FOR.");
                    return;
                }
            }

            int connectorCol = loopNode.Column;
            int connectorRow = loopNode.Row +2;

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

            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.For,
                Name = $"Подготовка",
                Description = "Цикл 'FOR'",
                Id = Guid.NewGuid(),
                Shot = shot,
                Docs = docs,
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
                Type = BlockType.LoopConnector,
                Name = $"",
                Description = "Тело цикла FOR",
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

            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.TrueBranch);

            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);

            HighlightAvailableCells();
            BuildSyntaxTree();
        }

    }
}