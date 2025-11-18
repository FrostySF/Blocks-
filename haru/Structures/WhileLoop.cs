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
        // Добавьте этот код в ext.cs, заменив существующие методы CreateWhileLoopStructure

        public void CreateWhileLoopStructure(double startX, double startY)
        {
            SaveState();

            // 1. Вычисляем координаты сетки для блока условия (ромб)
            int loopCol = (int)Math.Round(startX / (double)GRID_STEP);
            int loopRow = (int)Math.Round(startY / (double)GRID_STEP);

            loopRow = Math.Max(0, Math.Min(GRID_ROWS - 1, loopRow));
            loopCol = Math.Max(0, Math.Min(GRID_COLUMNS - 1, loopCol));

            GridNode loopNode = virtualGrid[loopRow, loopCol];

            // Если ячейка занята, ищем свободную
            if (!loopNode.IsAvailable)
            {
                loopNode = FindNearestFreeNode(loopRow, loopCol);
                if (loopNode == null)
                {
                    ShowNotification("Нет места для блока условия цикла.");
                    return;
                }
            }

            // 2. Определяем ячейку для соединителя (круг) - справа от условия
            int connectorCol = loopNode.Column + 2;
            int connectorRow = loopNode.Row;

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

            // 3. Создаем блок условия цикла (WHILE - ромб)
            blockCounter++;
            var loopBlock = new BlockItem
            {
                Type = BlockType.While,
                Name = $"Пока {blockCounter}",
                Description = "Условие цикла 'ПОКА'",
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
                Description = "Тело цикла",
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

            ShowNotification($"Структура цикла WHILE создана:\n- Условие: {loopBlock.Name}\n- Соединитель: {connectorBlock.Name}\n\nВыход из цикла (Нет) - вниз от условия.");
        }

        public void CreateWhileLoopStructureInViewport()
        {
            SaveState();
            GridNode loopNode = FindFirstFreeGridNodeInViewport();
            if (loopNode == null)
            {
                ShowNotification("Свободные ячейки для блоков отсутствуют.");
                return;
            }

            int connectorCol = loopNode.Column + 2;
            int connectorRow = loopNode.Row;

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
                Type = BlockType.While,
                Name = $"Пока {blockCounter}",
                Description = "Условие цикла 'ПОКА'",
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
                Description = "Тело цикла",
                Code = "", // Пустой код
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
            CreateManualConnection(loopBlock, connectorBlock, ConnectionType.LoopBody);
            CreateManualConnection(connectorBlock, loopBlock, ConnectionType.Normal);
            HighlightAvailableCells();
            BuildSyntaxTree();

            ShowNotification($"Структура цикла WHILE создана в области видимости:\n- Условие: {loopBlock.Name}\n- Соединитель: {connectorBlock.Name}\n\nВыход из цикла (Нет) - вниз от условия.\nТеперь можно добавлять блоки внутрь цикла!");
        }
    }
}
