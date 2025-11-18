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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {

        private Point GetAnchorPosition(BlockItem block, ConnectionType type, bool isOutput)
        {
            // Находим визуальный Border блока на Canvas
            Border blockBorder = BlocksCanvas.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Tag == block);

            // Если визуального блока нет, возвращаем центр блока по координатам Canvas
            if (blockBorder == null)
                return new Point(block.CanvasLeft + 50, block.CanvasTop + 30);

            double left = Canvas.GetLeft(blockBorder);
            double top = Canvas.GetTop(blockBorder);
            double width = blockBorder.Width;
            double height = blockBorder.Height;

            // Входные якоря (линии идут к блоку)
            if (!isOutput)
            {
                return block.Type switch
                {
                    BlockType.While => type switch
                    {
                        ConnectionType.LoopBody => new Point(left, top + height * 0.5),      // левый - вход из тела
                        ConnectionType.TrueBranch => new Point(left + width * 0.5, top + height), // нижний - тело цикла
                        ConnectionType.FalseBranch => new Point(left + width, top + height * 0.5), // правый - выход из цикла
                        _ => new Point(left + width * 0.5, top) // стандартный вход сверху
                    },
                    BlockType.For => type switch
                    {
                        ConnectionType.LoopBody => new Point(left, top + height * 0.5),      // левый - обратная связь
                        ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5), // правый - тело
                        ConnectionType.FalseBranch => new Point(left + width * 0.5, top + height), // нижний - выход
                        _ => new Point(left + width * 0.5, top)
                    },
                    BlockType.DoWhile => type switch
                    {
                        ConnectionType.LoopBody => new Point(left, top + height * 0.5),      // левый - вход тела
                        ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5), // правый - повтор
                        ConnectionType.FalseBranch => new Point(left + width * 0.5, top + height), // нижний - выход
                        _ => new Point(left + width * 0.5, top)
                    },
                    BlockType.LoopConnector => type switch
                    {
                        ConnectionType.Normal => new Point(left, top + height * 0.5), // левый - обратно к циклу
                        _ => new Point(left + width * 0.5, top)
                    },
                    BlockType.Decision => type switch
                    {
                        ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5),
                        ConnectionType.FalseBranch => new Point(left, top + height * 0.5),
                        ConnectionType.Normal => new Point(left + width * 0.5, top + height),
                        _ => new Point(left + width * 0.5, top)
                    },
                    _ => new Point(left + width * 0.5, top) // стандартный вход сверху для всех остальных
                };
            }

            // Выходные якоря (линии идут от блока)
            return block.Type switch
            {
                BlockType.While => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width * 0.5, top + height),   // нижний - к телу
                    ConnectionType.FalseBranch => new Point(left + width, top + height * 0.5), // правый - выход
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5),            // левый - из тела
                    _ => new Point(left + width * 0.5, top + height)
                },
                BlockType.For => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5),  // правый - к телу
                    ConnectionType.FalseBranch => new Point(left + width * 0.5, top + height), // нижний - выход
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5),            // левый - обратная связь
                    _ => new Point(left + width * 0.5, top + height)
                },
                BlockType.DoWhile => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5),  // правый - повтор
                    ConnectionType.FalseBranch => new Point(left + width * 0.5, top + height), // нижний - выход
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5),            // левый - вход тела
                    _ => new Point(left + width * 0.5, top + height)
                },
                BlockType.LoopConnector => type switch
                {
                    ConnectionType.Normal => new Point(left, top + height * 0.5), // левый - обратно к циклу
                    _ => new Point(left + width * 0.5, top + height)
                },
                BlockType.Decision => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5),
                    ConnectionType.FalseBranch => new Point(left, top + height * 0.5),
                    ConnectionType.Normal => new Point(left + width * 0.5, top + height),
                    _ => new Point(left + width * 0.5, top + height)
                },
                _ => new Point(left + width * 0.5, top + height) // стандартный выход снизу для обычных блоков
            };
        }

        // Вспомогательный метод для поиска ближайшего входного якоря
        private Ellipse FindNearestAvailableAnchor(Point position, double threshold)
        {
            // Ищем ближайший якорь для ПРИСОЕДИНЕНИЯ (то есть ВХОДНОЙ якорь)
            foreach (var blockItem in listofblocks)
            {
                if (blockItem == connectionStartBlock) continue; // Нельзя подключить блок к себе же

                var blockBorder = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == blockItem);
                if (blockBorder?.Child is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Border hitBox && hitBox.Child is Ellipse anchor)
                        {
                            // Проверяем, что это не исходящий якорь, а входной (ConnectionType.Normal используется для входа)
                            if (hitBox.Tag is ValueTuple<BlockItem, ConnectionType> tag && tag.Item2 == ConnectionType.Normal)
                            {
                                Point anchorCenter = GetAnchorPosition(blockItem, ConnectionType.Input, isOutput: false);

                                if (Distance(position, anchorCenter) < threshold)
                                {
                                    return anchor;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
        private void UpdateConnectionLines(BlockItem movedBlock)
        {
            var relatedLines = connectionLines
                .Where(cl => cl.FromBlock == movedBlock || cl.ToBlock == movedBlock)
                .ToList();

            foreach (var connection in relatedLines)
            {
                Point start = GetAnchorPosition(connection.FromBlock, connection.Type, isOutput: true);
                Point end = GetAnchorPosition(connection.ToBlock, ConnectionType.Input, isOutput: false);
                var newPoints = RoutePath(start, end);

                if (connection.VisualPath != null)
                {
                    var pc = new PointCollection();
                    foreach (var p in newPoints) pc.Add(p);
                    connection.VisualPath.Points = pc;
                }

                var pathPoints = RoutePath(start, end);

                // --- НОВЫЕ ПРОВЕРКИ КОЛЛИЗИЙ ---
                bool intersectsBlock = CheckPathBlockIntersection(pathPoints, connection.FromBlock, connection.ToBlock);
                bool overlapsLine = CheckPathLineOverlap(pathPoints, connection);
                // -------------------------------

                // 2. Update visual path
                if (connection.VisualPath == null)
                {
                    connection.VisualPath = new Polyline { StrokeThickness = 3 };
                    FlowchartCanvas.Children.Add(connection.VisualPath);
                }
                // --- ЛОГИКА ДОБАВЛЕНИЯ ПОДПИСИ "ДА"/"НЕТ" ---
                if (connection.Type == ConnectionType.TrueBranch || connection.Type == ConnectionType.FalseBranch)
                {
                    // 1. Определяем текст подписи
                    string labelText = connection.Type == ConnectionType.TrueBranch ? "Да" : "Нет";

                    // 2. Создаем TextBlock
                    var label = new TextBlock
                    {
                        Text = labelText,
                        Foreground = connection.Stroke, // Цвет линии
                        FontSize = 14
                    };

                    // 3. Позиционирование у якоря (Points[0])
                    if (connection.Points != null && connection.Points.Count > 0)
                    {
                        Point anchorPoint = connection.Points[0];

                        double offsetX;
                        double offsetY;

                        // Настройка смещения для расположения текста "у выхода"
                        const double TEXT_PADDING = 8.0;
                        const double TEXT_HEIGHT_OFFSET = 18.0;
                        const double TEXT_WIDTH_APPROX = 25.0;

                        if (connection.Type == ConnectionType.TrueBranch)
                        {
                            // Для "Да": Смещаем вправо от якоря и поднимаем
                            offsetX = TEXT_PADDING;
                            offsetY = -TEXT_HEIGHT_OFFSET;
                        }
                        else // ConnectionType.FalseBranch
                        {
                            // Для "Нет": Смещаем влево от якоря и поднимаем
                            offsetX = -(TEXT_WIDTH_APPROX + TEXT_PADDING);
                            offsetY = -TEXT_HEIGHT_OFFSET;
                        }

                        // Устанавливаем позицию на Canvas
                        Canvas.SetLeft(label, anchorPoint.X + offsetX);
                        Canvas.SetTop(label, anchorPoint.Y + offsetY);

                        // Добавляем на Canvas и сохраняем в модель
                        BlocksCanvas.Children.Add(label);
                        connection.VisualLabel = label;
                    }
                }


                // Устанавливаем цвет в зависимости от проверки коллизий
                if (intersectsBlock || overlapsLine)
                {
                    connection.VisualPath.Stroke = ErrorLineColor; // Красный цвет
                }
                else
                {
                    connection.VisualPath.Stroke = NormalLineColor; // Белый цвет
                }


                if (connection.ArrowHead != null)
                {
                    FlowchartCanvas.Children.Remove(connection.ArrowHead);
                    connection.ArrowHead = CreateArrowHeadForPath(newPoints,
                        connection.VisualPath.Stroke is SolidColorBrush brush ? brush.Color : Colors.White);
                    FlowchartCanvas.Children.Add(connection.ArrowHead);
                }
            }
        }


        /// <summary>
        /// Основной метод построения маршрута между двумя точками
        /// </summary>
        private List<Point> RoutePath(Point start, Point end)
        {
            var points = new List<Point> { start };

            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double absDeltaX = Math.Abs(deltaX);
            double absDeltaY = Math.Abs(deltaY);

            // 1. Прямая линия для коротких расстояний
            if (Distance(start, end) < MIN_SEGMENT_LENGTH * 2)
            {
                points.Add(end);
                return points;
            }

            // 2. Проверка на вертикальное выравнивание
            if (absDeltaX < GRID_STEP / 2)
            {
                // Только вертикальная линия
                if (absDeltaY < GRID_STEP / 2)
                {
                    // Точки совпадают
                    points.Add(end);
                }
                else
                {
                    // Чистая вертикальная линия
                    points.Add(new Point(start.X, end.Y));
                    if (Math.Abs(end.X - start.X) > 0.1)
                    {
                        points.Add(end);
                    }
                }
                return points;
            }

            // 3. Проверка на горизонтальное выравнивание
            if (absDeltaY < GRID_STEP / 2)
            {
                // Только горизонтальная линия
                points.Add(new Point(end.X, start.Y));
                if (Math.Abs(end.Y - start.Y) > 0.1)
                {
                    points.Add(end);
                }
                return points;
            }

            // 4. Определение типа маршрута по ГОСТ (Z-образный или L-образный)
            bool useHorizontalFirst = absDeltaX >= absDeltaY;

            // Сначала пробуем стандартный Z-маршрут (по центру)
            List<Point> route = useHorizontalFirst
                ? BuildHVHRoute(start, end, deltaX, deltaY)
                : BuildVHVRoute(start, end, deltaX, deltaY);

            // 5. Проверка на блокировку - только если заблокирован, ищем обход
            if (IsRouteBlocked(route))
            {
                route = BuildObstacleAvoidingRoute(start, end, deltaX, deltaY, useHorizontalFirst);
            }

            return route;
        }

        /// <summary>
        /// Построение H-V-H маршрута (горизонталь-вертикаль-горизонталь)
        /// </summary>
        private List<Point> BuildHVHRoute(Point start, Point end, double deltaX, double deltaY)
        {
            var points = new List<Point> { start };

            // Средняя точка по X (с привязкой к сетке)
            double midX = SnapToGrid(start.X + deltaX / 2.0);

            // Первый поворот - горизонтальный сегмент
            points.Add(new Point(midX, start.Y));

            // Второй поворот - вертикальный сегмент
            points.Add(new Point(midX, end.Y));

            // Конечная точка (последний горизонтальный сегмент)
            points.Add(end);

            return points;
        }

        /// <summary>
        /// Построение V-H-V маршрута (вертикаль-горизонталь-вертикаль)
        /// </summary>
        private List<Point> BuildVHVRoute(Point start, Point end, double deltaX, double deltaY)
        {
            var points = new List<Point> { start };

            // Средняя точка по Y (с привязкой к сетке)
            double midY = SnapToGrid(start.Y + deltaY / 2.0);

            // Первый поворот - вертикальный сегмент
            points.Add(new Point(start.X, midY));

            // Второй поворот - горизонтальный сегмент
            points.Add(new Point(end.X, midY));

            // Конечная точка (последний вертикальный сегмент)
            points.Add(end);

            return points;
        }



        /// <summary>
        /// Построение маршрута с обходом препятствий
        /// </summary>
        private List<Point> BuildObstacleAvoidingRoute(Point start, Point end, double deltaX, double deltaY, bool horizontalFirst)
        {
            double dirX = deltaX >= 0 ? 1.0 : -1.0;
            double dirY = deltaY >= 0 ? 1.0 : -1.0;

            // Пробуем альтернативные точки изгиба
            List<List<Point>> candidateRoutes = new List<List<Point>>();

            if (horizontalFirst)
            {
                // Пробуем разные позиции по X для H-V-H маршрута
                double[] offsets = {
            deltaX * 0.25,  // Ближе к старту
            deltaX * 0.33,
            deltaX * 0.67,
            deltaX * 0.75,  // Ближе к концу
            dirX * GRID_STEP * 2,  // Минимальный отступ
            dirX * GRID_STEP * 3
        };

                foreach (double offset in offsets)
                {
                    var route = TryHVHRouteWithOffset(start, end, offset, deltaY);
                    if (!IsRouteBlocked(route))
                    {
                        return route; // Возвращаем первый рабочий вариант
                    }
                    candidateRoutes.Add(route);
                }
            }
            else
            {
                // Пробуем разные позиции по Y для V-H-V маршрута
                double[] offsets = {
            deltaY * 0.25,
            deltaY * 0.33,
            deltaY * 0.67,
            deltaY * 0.75,
            dirY * GRID_STEP * 2,
            dirY * GRID_STEP * 3
        };

                foreach (double offset in offsets)
                {
                    var route = TryVHVRouteWithOffset(start, end, deltaX, offset);
                    if (!IsRouteBlocked(route))
                    {
                        return route;
                    }
                    candidateRoutes.Add(route);
                }
            }

            // Если ничего не подошло - пробуем широкий боковой обход
            var bypassRoute = BuildSideBypassRoute(start, end, horizontalFirst, dirX, dirY);
            if (!IsRouteBlocked(bypassRoute))
            {
                return bypassRoute;
            }

            // В крайнем случае возвращаем первый кандидат (наименее плохой)
            return candidateRoutes.Count > 0 ? candidateRoutes[0] : bypassRoute;
        }

        /// <summary>
        /// Построение H-V-H маршрута с заданным смещением
        /// </summary>
        private List<Point> TryHVHRouteWithOffset(Point start, Point end, double offsetX, double deltaY)
        {
            var points = new List<Point> { start };
            double midX = SnapToGrid(start.X + offsetX);
            points.Add(new Point(midX, start.Y));
            points.Add(new Point(midX, end.Y));
            points.Add(end);
            return points;
        }

        /// <summary>
        /// Построение V-H-V маршрута с заданным смещением
        /// </summary>
        private List<Point> TryVHVRouteWithOffset(Point start, Point end, double deltaX, double offsetY)
        {
            var points = new List<Point> { start };
            double midY = SnapToGrid(start.Y + offsetY);
            points.Add(new Point(start.X, midY));
            points.Add(new Point(end.X, midY));
            points.Add(end);
            return points;
        }

        /// <summary>
        /// Построение бокового обхода препятствия
        /// </summary>
        private List<Point> BuildSideBypassRoute(Point start, Point end, bool horizontalFirst, double dirX, double dirY)
        {
            var points = new List<Point> { start };

            if (horizontalFirst)
            {
                // Отступ вбок
                double offsetX = SnapToGrid(start.X + dirX * GRID_STEP * 2);
                points.Add(new Point(offsetX, start.Y));

                // Обход по вертикали с запасом
                double offsetY = SnapToGrid(start.Y + dirY * GRID_STEP * 4);
                points.Add(new Point(offsetX, offsetY));

                // К целевому X
                points.Add(new Point(end.X, offsetY));

                // К цели
                points.Add(new Point(end.X, end.Y));
            }
            else
            {
                // Отступ вниз/вверх
                double offsetY = SnapToGrid(start.Y + dirY * GRID_STEP * 2);
                points.Add(new Point(start.X, offsetY));

                // Обход по горизонтали с запасом
                double offsetX = SnapToGrid(start.X + dirX * GRID_STEP * 4);
                points.Add(new Point(offsetX, offsetY));

                // К целевому Y
                points.Add(new Point(offsetX, end.Y));

                // К цели
                points.Add(new Point(end.X, end.Y));
            }

            points.Add(end);
            return points;
        }

        /// <summary>
        /// Проверка блокировки отдельного сегмента пути
        /// </summary>
        private bool IsPathBlocked(Point start, Point end)
        {
            // Создаем прямоугольник вокруг сегмента с небольшим отступом
            var rect = new Rect(
                Math.Min(start.X, end.X) - OBSTACLE_CLEARANCE,
                Math.Min(start.Y, end.Y) - OBSTACLE_CLEARANCE,
                Math.Abs(end.X - start.X) + OBSTACLE_CLEARANCE * 2,
                Math.Abs(end.Y - start.Y) + OBSTACLE_CLEARANCE * 2
            );

            foreach (var child in BlocksCanvas.Children)
            {
                if (child is Border border && border.Tag is BlockItem block)
                {
                    var blockRect = new Rect(
                        Canvas.GetLeft(border) - OBSTACLE_CLEARANCE / 2,
                        Canvas.GetTop(border) - OBSTACLE_CLEARANCE / 2,
                        border.Width + OBSTACLE_CLEARANCE,
                        border.Height + OBSTACLE_CLEARANCE
                    );

                    if (DoRectsIntersect(rect, blockRect))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Проверка блокировки всего маршрута
        /// </summary>
        private bool IsRouteBlocked(List<Point> route)
        {
            for (int i = 0; i < route.Count - 1; i++)
            {
                if (IsPathBlocked(route[i], route[i + 1]))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Проверка пересечения двух прямоугольников
        /// </summary>
        private bool DoRectsIntersect(Rect rect1, Rect rect2)
        {
            return !(rect1.Right < rect2.Left ||
                     rect1.Left > rect2.Right ||
                     rect1.Bottom < rect2.Top ||
                     rect1.Top > rect2.Bottom);
        }

        private void CreateManualConnection(BlockItem from, BlockItem to, ConnectionType type)
        {
            SaveState();

            if (from == to)
                return;
            if (connectionLines.Any(c => c.FromBlock == from && c.ToBlock == to && c.Type == type))
                return;

            bool exists = connectionLines.Any(cl =>
                cl.FromBlock == from && cl.ToBlock == to && cl.Type == type);

            if (exists) return;
            Point startAnchor = GetAnchorPosition(from, type, isOutput: true);

            ConnectionType endType = ConnectionType.Input;
            if ((to.Type == BlockType.While || to.Type == BlockType.For || to.Type == BlockType.DoWhile) && type == ConnectionType.LoopBody)
            {
                endType = ConnectionType.LoopBody;
            }


            Point endAnchor = GetAnchorPosition(to, endType, isOutput: false);

            var points = RoutePath(startAnchor, endAnchor);
            var polyline = new Polyline
            {
                Stroke = type switch
                {
                    ConnectionType.TrueBranch => new SolidColorBrush(Colors.LimeGreen),
                    ConnectionType.FalseBranch => new SolidColorBrush(Colors.IndianRed),
                    ConnectionType.LoopBody => new SolidColorBrush(Colors.DeepSkyBlue),
                    _ => new SolidColorBrush(Colors.White)
                },
                StrokeThickness = 2,
                StrokeLineJoin = PenLineJoin.Round
            };

            polyline.Points = new PointCollection();
            foreach (var p in points)
                polyline.Points.Add(p);

            polyline.RightTapped += Polyline_RightTapped;
            FlowchartCanvas.Children.Add(polyline);

            // Стрелка
            var arrow = CreateArrowHeadForPath(points,
                type switch
                {
                    ConnectionType.TrueBranch => Colors.LimeGreen,
                    ConnectionType.FalseBranch => Colors.IndianRed,
                    ConnectionType.LoopBody => Colors.DeepSkyBlue,
                    _ => Colors.White
                });

            FlowchartCanvas.Children.Add(arrow);

            var connection = new ConnectionLine
            {
                FromBlock = from,
                ToBlock = to,
                Type = type,
                VisualPath = polyline,
                ArrowHead = arrow
            };

            // --- ЛОГИКА ДОБАВЛЕНИЯ ПОДПИСИ "ДА"/"НЕТ" ---
            if (connection.Type == ConnectionType.TrueBranch || connection.Type == ConnectionType.FalseBranch)
            {
                // 1. Определяем текст подписи
                string labelText = connection.Type == ConnectionType.TrueBranch ? "Да" : "Нет";

                var label = new TextBlock
                {
                    Text = labelText,
                    Foreground = connection.Stroke,
                    FontSize = 14
                };

                // 3. Позиционирование у якоря (Points[0])
                if (connection.Points != null && connection.Points.Count > 0)
                {
                    Point anchorPoint = connection.Points[0];

                    double offsetX;
                    double offsetY;

                    // Настройка смещения для расположения текста "у выхода"
                    const double TEXT_PADDING = 8.0;
                    const double TEXT_HEIGHT_OFFSET = 18.0;
                    const double TEXT_WIDTH_APPROX = 25.0;

                    if (connection.Type == ConnectionType.TrueBranch)
                    {
                        // Для "Да": Смещаем вправо от якоря и поднимаем
                        offsetX = TEXT_PADDING;
                        offsetY = -TEXT_HEIGHT_OFFSET;
                    }
                    else // ConnectionType.FalseBranch
                    {
                        // Для "Нет": Смещаем влево от якоря и поднимаем
                        offsetX = -(TEXT_WIDTH_APPROX + TEXT_PADDING);
                        offsetY = -TEXT_HEIGHT_OFFSET;
                    }

                    // Устанавливаем позицию на Canvas
                    Canvas.SetLeft(label, anchorPoint.X + offsetX);
                    Canvas.SetTop(label, anchorPoint.Y + offsetY);

                    // Добавляем на Canvas и сохраняем в модель
                    BlocksCanvas.Children.Add(label);
                    connection.VisualLabel = label;
                }
            }

            connectionLines.Add(connection);
            BuildSyntaxTree();
        }

        private GridNode FindFirstFreeGridNodeInViewport()
        {
            double viewportLeft = MainScrollViewer.HorizontalOffset;
            double viewportTop = MainScrollViewer.VerticalOffset;
            double viewportRight = viewportLeft + MainScrollViewer.ViewportWidth;
            double viewportBottom = viewportTop + MainScrollViewer.ViewportHeight;

            // Конвертируем в координаты сетки
            int startCol = Math.Max(0, (int)(viewportLeft / GRID_STEP));
            int endCol = Math.Min(GRID_COLUMNS - 1, (int)(viewportRight / GRID_STEP));
            int startRow = Math.Max(0, (int)(viewportTop / GRID_STEP));
            int endRow = Math.Min(GRID_ROWS - 1, (int)(viewportBottom / GRID_STEP));

            if (!listofblocks.Any())
            {
                int centerRow = (startRow + endRow) / 2;
                int centerCol = (startCol + endCol) / 2;
                if (virtualGrid[centerRow, centerCol].IsAvailable)
                    return virtualGrid[centerRow, centerCol];
            }

            int centerR = (startRow + endRow) / 2;
            int centerC = (startCol + endCol) / 2;

            return FindNearestFreeNodeInRange(centerR, centerC, startRow, endRow, startCol, endCol);
        }

        private GridNode FindNearestFreeNodeInRange(int startR, int startC, int minR, int maxR, int minC, int maxC)
        {
            var visited = new bool[GRID_ROWS, GRID_COLUMNS];
            var q = new Queue<(int r, int c)>();
            q.Enqueue((startR, startC));
            visited[startR, startC] = true;

            (int r, int c)[] dirs = new (int, int)[]
            {
                (0,1),(1,0),(0,-1),(-1,0),
                (1,1),(1,-1),(-1,1),(-1,-1)
            };

            while (q.Count > 0)
            {
                var (r, c) = q.Dequeue();
                if (r >= minR && r <= maxR && c >= minC && c <= maxC)
                {
                    if (virtualGrid[r, c].IsAvailable) return virtualGrid[r, c];
                }

                foreach (var d in dirs)
                {
                    int nr = r + d.r, nc = c + d.c;
                    if (nr >= 0 && nr < GRID_ROWS && nc >= 0 && nc < GRID_COLUMNS && !visited[nr, nc])
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }

            return FindNearestFreeNode(startR, startC);
        }


        private GridNode FindNearestFreeNode(int startRow, int startCol)
        {
            // Обеспечиваем, что начальная точка находится в границах
            int rStart = Math.Max(0, Math.Min(GRID_ROWS - 1, startRow));
            int cStart = Math.Max(0, Math.Min(GRID_COLUMNS - 1, startCol));

            var visited = new bool[GRID_ROWS, GRID_COLUMNS];
            var q = new Queue<(int r, int c)>();
            q.Enqueue((rStart, cStart));
            visited[rStart, cStart] = true;

            // Направления для BFS (включая диагональные)
            (int r, int c)[] dirs = new (int, int)[]
            {
                (0,1),(1,0),(0,-1),(-1,0),
                (1,1),(1,-1),(-1,1),(-1,-1)
            };

            while (q.Count > 0)
            {
                var (r, c) = q.Dequeue();
                if (virtualGrid[r, c].IsAvailable) return virtualGrid[r, c];
                foreach (var d in dirs)
                {
                    int nr = r + d.r, nc = c + d.c;
                    if (nr >= 0 && nr < GRID_ROWS && nc >= 0 && nc < GRID_COLUMNS && !visited[nr, nc])
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }
            return null;
        }

        private bool CheckCollisionAtTemporaryLocation(BlockItem block)
        {
            // 1. Находим Border, связанный с блоком, чтобы временно изменить его позицию на Canvas
            var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);

            // Сохраняем и устанавливаем временную позицию на Canvas
            double originalLeft = -1, originalTop = -1;
            if (border != null)
            {
                originalLeft = Canvas.GetLeft(border);
                originalTop = Canvas.GetTop(border);

                // Временно устанавливаем визуальную позицию для корректной работы GetAnchorPosition
                Canvas.SetLeft(border, block.CanvasLeft);
                Canvas.SetTop(border, block.CanvasTop);
            }

            // 2. Проверяем коллизии линий (повторяем логику из UpdateConnectionLines)
            bool collisionDetected = false;
            var relatedLines = connectionLines
                .Where(cl => cl.FromBlock == block || cl.ToBlock == block)
                .ToList();

            foreach (var connection in relatedLines)
            {
                Point start = GetAnchorPosition(connection.FromBlock, connection.Type, isOutput: true);
                Point end = GetAnchorPosition(connection.ToBlock, ConnectionType.Input, isOutput: false);
                var pathPoints = RoutePath(start, end);

                // Предполагаем, что CheckPathBlockIntersection и CheckPathLineOverlap определены
                bool intersectsBlock = CheckPathBlockIntersection(pathPoints, connection.FromBlock, connection.ToBlock);
                bool overlapsLine = CheckPathLineOverlap(pathPoints, connection);

                if (intersectsBlock || overlapsLine)
                {
                    collisionDetected = true;
                    break; // Если найдена одна коллизия, этого достаточно
                }
            }

            // 3. Восстанавливаем оригинальную визуальную позицию
            if (border != null)
            {
                Canvas.SetLeft(border, originalLeft);
                Canvas.SetTop(border, originalTop);
            }

            return collisionDetected;
        }

        // BFS для поиска ближайшей свободной ячейки БЕЗ коллизий линий
        private GridNode FindNearestFreeNodeWithoutCollisions(BlockItem block, int startRow, int startCol)
        {
            int rStart = Math.Max(0, Math.Min(GRID_ROWS - 1, startRow));
            int cStart = Math.Max(0, Math.Min(GRID_COLUMNS - 1, startCol));

            var visited = new bool[GRID_ROWS, GRID_COLUMNS];
            var q = new Queue<(int r, int c)>();
            q.Enqueue((rStart, cStart));
            visited[rStart, cStart] = true;

            // Направления для BFS (включая диагональные)
            (int r, int c)[] dirs = new (int, int)[]
            {
                (0,1),(1,0),(0,-1),(-1,0),
                (1,1),(1,-1),(-1,1),(-1,-1)
            };

            while (q.Count > 0)
            {
                var (r, c) = q.Dequeue();
                GridNode currentNode = virtualGrid[r, c];

                // 1. Проверяем, свободна ли ячейка (block-to-block)
                if (currentNode.IsAvailable)
                {
                    // 2. Временно устанавливаем логическую позицию для проверки line-to-block
                    block.CanvasLeft = currentNode.Column * GRID_STEP;
                    block.CanvasTop = currentNode.Row * GRID_STEP;

                    // 3. Проверяем коллизии линий
                    if (!CheckCollisionAtTemporaryLocation(block))
                    {
                        // Если нет коллизий, возвращаем эту ячейку
                        return currentNode;
                    }
                }

                // 4. Добавляем непосещенных соседей в очередь
                foreach (var d in dirs)
                {
                    int nr = r + d.r, nc = c + d.c;
                    if (nr >= 0 && nr < GRID_ROWS && nc >= 0 && nc < GRID_COLUMNS && !visited[nr, nc])
                    {
                        visited[nr, nc] = true;
                        q.Enqueue((nr, nc));
                    }
                }
            }

            // Возвращаем null, если свободное место без коллизий не найдено
            return null;
        }
        private Polygon CreateArrowHeadForPath(IList<Point> points, Windows.UI.Color color)
        {
            if (points == null || points.Count < 2)
                return null;

            var end = points[points.Count - 1];
            var prev = points[points.Count - 2];
            double angle = Math.Atan2(end.Y - prev.Y, end.X - prev.X) * 180.0 / Math.PI;

            var arrow = new Polygon
            {
                Points = new PointCollection
        {
            new Point(0, 0),
            new Point(-12, -6),
            new Point(-12, 6)
        },
                Fill = new SolidColorBrush(color),
                RenderTransform = new RotateTransform
                {
                    Angle = angle,
                    CenterX = 0,
                    CenterY = 0
                }
            };
            Canvas.SetLeft(arrow, end.X);
            Canvas.SetTop(arrow, end.Y);

            return arrow;
        }

        private bool IsLineSegmentCloseToRect(Point p1, Point p2, Rect rect, double threshold)
        {
            double x_min = Math.Min(p1.X, p2.X) - threshold;
            double x_max = Math.Max(p1.X, p2.X) + threshold;
            double y_min = Math.Min(p1.Y, p2.Y) - threshold;
            double y_max = Math.Max(p1.Y, p2.Y) + threshold;

            if (rect.Right < x_min || rect.Left > x_max || rect.Bottom < y_min || rect.Top > y_max)
            {
                return false;
            }

            if (Math.Abs(p1.X - p2.X) < 10)
            {
                return rect.Left <= p1.X + threshold && rect.Right >= p1.X - threshold;
            }
            else if (Math.Abs(p1.Y - p2.Y) < 10)
            {
                return rect.Top <= p1.Y + threshold && rect.Bottom >= p1.Y - threshold;
            }

            return false;
        }
    }
}
