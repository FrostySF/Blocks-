using Blocks_.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        // Длина перпендикулярного сегмента при выходе/входе
        private const double PERPENDICULAR_SEGMENT_LENGTH = 20.0;

        private List<Point> RoutePath(Point start, Point end, ConnectionType connectionType = ConnectionType.Normal, BlockItem fromBlock = null)
        {
            bool isLoopBack = connectionType == ConnectionType.LoopBody && start.Y >= end.Y - SettingsWindow.AppSettings.GridStep;
            if (isLoopBack)
            {
                return RouteLoopbackPath(start, end);
            }
            bool isLoopExit = (connectionType == ConnectionType.FalseBranch || connectionType == ConnectionType.LoopExit)
                              && start.Y < end.Y
                              && fromBlock != null
                              && (fromBlock.Type == BlockType.While ||
                                  fromBlock.Type == BlockType.DoWhile ||
                                  fromBlock.Type == BlockType.For);

            if (isLoopExit)
            {
                return RouteRightBypassPath(start, end);
            }

            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;

            if (connectionType == ConnectionType.TrueBranch || connectionType == ConnectionType.FalseBranch)
            {
                if (Math.Abs(deltaX) > SettingsWindow.AppSettings.GridStep / 2)
                {
                    return RouteSideExitPath(start, end, deltaX > 0);
                }
            }

            return RoutePathInternal(start, end, connectionType, fromBlock);
        }

        private List<Point> RouteSideExitPath(Point start, Point end, bool goingRight)
        {
            var points = new List<Point> { start };

            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double absDeltaX = Math.Abs(deltaX);
            double absDeltaY = Math.Abs(deltaY);

            double sideOffset = SettingsWindow.AppSettings.GridStep * 0.5;

            if (absDeltaY < SettingsWindow.AppSettings.GridStep * 2)
            {
                double midX = start.X + (goingRight ? sideOffset : -sideOffset);

                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
                points.Add(end);
                return points;
            }

            if (absDeltaX > SettingsWindow.AppSettings.GridStep * 1.5)
            {
                double midX = start.X + deltaX * 0.5;
                midX = SnapToGrid(midX);

                points.Add(new Point(midX, start.Y));
                points.Add(new Point(midX, end.Y));
                points.Add(end);
                return points;
            }

            double exitX = start.X + (goingRight ? sideOffset : -sideOffset);
            double midY = start.Y + deltaY * 0.5;
            midY = SnapToGrid(midY);

            points.Add(new Point(exitX, start.Y));
            points.Add(new Point(exitX, midY));
            points.Add(new Point(end.X, midY));
            points.Add(end);

            return points;
        }

        private List<Point> RoutePathInternal(Point start, Point end, ConnectionType connectionType = ConnectionType.Normal, BlockItem fromBlock = null)
        {
            var points = new List<Point> { start };

            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double absDeltaX = Math.Abs(deltaX);
            double absDeltaY = Math.Abs(deltaY);

            // Определяем направление выхода из блока
            bool exitFromSide = IsExitFromSide(start, end, fromBlock, connectionType);
            bool exitFromTopBottom = IsExitFromTopBottom(start, end, fromBlock, connectionType);

            // Добавляем перпендикулярный сегмент при выходе
            if (exitFromSide)
            {
                // Выход сбоку - добавляем горизонтальный сегмент
                double horizontalOffset = Math.Sign(deltaX) * PERPENDICULAR_SEGMENT_LENGTH;
                Point firstSegmentEnd = new Point(start.X + horizontalOffset, start.Y);
                points.Add(firstSegmentEnd);
                start = firstSegmentEnd; // Обновляем стартовую точку для дальнейшего маршрутирования
            }
            else if (exitFromTopBottom)
            {
                // Выход сверху/снизу - добавляем вертикальный сегмент
                double verticalOffset = Math.Sign(deltaY) * PERPENDICULAR_SEGMENT_LENGTH;
                Point firstSegmentEnd = new Point(start.X, start.Y + verticalOffset);
                points.Add(firstSegmentEnd);
                start = firstSegmentEnd; // Обновляем стартовую точку для дальнейшего маршрутирования
            }

            // Обновляем дельты после добавления перпендикулярного сегмента
            deltaX = end.X - start.X;
            deltaY = end.Y - start.Y;
            absDeltaX = Math.Abs(deltaX);
            absDeltaY = Math.Abs(deltaY);

            // Определяем направление входа в блок
            bool enterToSide = IsEnterToSide(end, fromBlock, connectionType);
            bool enterToTopBottom = IsEnterToTopBottom(end, fromBlock, connectionType);

            // Создаем промежуточную конечную точку для перпендикулярного входа
            Point adjustedEnd = end;
            if (enterToSide)
            {
                // Вход сбоку - отступаем по горизонтали
                double horizontalOffset = -Math.Sign(deltaX) * PERPENDICULAR_SEGMENT_LENGTH;
                adjustedEnd = new Point(end.X + horizontalOffset, end.Y);
            }
            else if (enterToTopBottom)
            {
                // Вход сверху/снизу - отступаем по вертикали
                double verticalOffset = -Math.Sign(deltaY) * PERPENDICULAR_SEGMENT_LENGTH;
                adjustedEnd = new Point(end.X, end.Y + verticalOffset);
            }

            // Теперь выполняем стандартную логику маршрутизации между start и adjustedEnd
            if (absDeltaX < 1 && absDeltaY > SettingsWindow.AppSettings.GridStep)
            {
                points.Add(adjustedEnd);
            }
            else if (absDeltaY < 1 && absDeltaX > SettingsWindow.AppSettings.GridStep)
            {
                points.Add(adjustedEnd);
            }
            else if (Distance(start, adjustedEnd) < SettingsWindow.AppSettings.MinSegmentLength * 1.5)
            {
                points.Add(new Point(start.X, adjustedEnd.Y));
                points.Add(adjustedEnd);
            }
            else if (absDeltaX < SettingsWindow.AppSettings.GridStep / 2)
            {
                if (absDeltaY < SettingsWindow.AppSettings.GridStep / 2)
                {
                    points.Add(adjustedEnd);
                }
                else
                {
                    points.Add(new Point(start.X, adjustedEnd.Y));
                    if (Math.Abs(adjustedEnd.X - start.X) > 0.1) points.Add(adjustedEnd);
                }
            }
            else if (absDeltaY < SettingsWindow.AppSettings.GridStep / 2)
            {
                points.Add(new Point(adjustedEnd.X, start.Y));
                if (Math.Abs(adjustedEnd.Y - start.Y) > 0.1) points.Add(adjustedEnd);
            }
            else
            {
                bool useHorizontalFirst = ShouldUseHorizontalFirst(start, adjustedEnd, deltaX, deltaY);
                var routePoints = TryMultipleRoutingStrategies(start, adjustedEnd, deltaX, deltaY, useHorizontalFirst);

                // Добавляем точки маршрута, пропуская первую точку (она уже есть как start)
                for (int i = 1; i < routePoints.Count; i++)
                {
                    points.Add(routePoints[i]);
                }
            }

            // Добавляем финальный перпендикулярный сегмент для входа
            if (enterToSide || enterToTopBottom)
            {
                points.Add(end);
            }

            return points;
        }

        private bool IsExitFromSide(Point start, Point end, BlockItem fromBlock, ConnectionType connectionType)
        {
            if (fromBlock == null) return false;

            // Определяем, выходит ли линия сбоку от блока
            var exitDirection = GetExitDirection(fromBlock, connectionType);

            // Для боковых выходов (лево/право) возвращаем true
            return exitDirection == ExitDirection.Left || exitDirection == ExitDirection.Right;
        }

        private bool IsExitFromTopBottom(Point start, Point end, BlockItem fromBlock, ConnectionType connectionType)
        {
            if (fromBlock == null) return false;

            // Определяем, выходит ли линия сверху/снизу от блока
            var exitDirection = GetExitDirection(fromBlock, connectionType);

            // Для вертикальных выходов (верх/низ) возвращаем true
            return exitDirection == ExitDirection.Top || exitDirection == ExitDirection.Bottom;
        }

        private bool IsEnterToSide(Point endPoint, BlockItem toBlock, ConnectionType connectionType)
        {
            if (toBlock == null) return false;

            // Определяем направление входа для целевого блока
            // Для простоты считаем, что если блок имеет боковые входы, то это боковой вход
            // В реальном приложении нужно учитывать тип блока и тип соединения
            return toBlock.Type == BlockType.While ||
                   toBlock.Type == BlockType.DoWhile ||
                   toBlock.Type == BlockType.For ||
                   toBlock.Type == BlockType.Decision ||
                   toBlock.Type == BlockType.LoopConnector ||
                   toBlock.Type == BlockType.DoLoopConnector;
        }

        private bool IsEnterToTopBottom(Point endPoint, BlockItem toBlock, ConnectionType connectionType)
        {
            if (toBlock == null) return true; // По умолчанию считаем вход сверху/снизу

            // Блоки, которые обычно имеют вход сверху
            return toBlock.Type == BlockType.Start ||
                   toBlock.Type == BlockType.Process ||
                   toBlock.Type == BlockType.Input ||
                   toBlock.Type == BlockType.Output ||
                   toBlock.Type == BlockType.InputOutput ||
                   toBlock.Type == BlockType.VariableDeclaration ||
                   toBlock.Type == BlockType.ArrayDeclaration;
        }

        private enum ExitDirection
        {
            Top,
            Bottom,
            Left,
            Right
        }

        private ExitDirection GetExitDirection(BlockItem block, ConnectionType connectionType)
        {
            if (block == null) return ExitDirection.Bottom;

            return block.Type switch
            {
                BlockType.While => connectionType switch
                {
                    ConnectionType.LoopBody => ExitDirection.Left,
                    ConnectionType.TrueBranch => ExitDirection.Bottom,
                    ConnectionType.FalseBranch => ExitDirection.Right,
                    _ => ExitDirection.Bottom
                },
                BlockType.DoWhile => connectionType switch
                {
                    ConnectionType.TrueBranch => ExitDirection.Right,
                    ConnectionType.FalseBranch => ExitDirection.Bottom,
                    ConnectionType.LoopBody => ExitDirection.Left,
                    _ => ExitDirection.Bottom
                },
                BlockType.For => connectionType switch
                {
                    ConnectionType.TrueBranch => ExitDirection.Bottom,
                    ConnectionType.FalseBranch => ExitDirection.Right,
                    _ => ExitDirection.Bottom
                },
                BlockType.LoopConnector => ExitDirection.Left,
                BlockType.DoLoopConnector => connectionType switch
                {
                    ConnectionType.Normal => ExitDirection.Bottom,
                    ConnectionType.LoopBody => ExitDirection.Left,
                    _ => ExitDirection.Bottom
                },
                BlockType.Decision => connectionType switch
                {
                    ConnectionType.TrueBranch => ExitDirection.Right,
                    ConnectionType.FalseBranch => ExitDirection.Left,
                    _ => ExitDirection.Right
                },
                _ => ExitDirection.Bottom // По умолчанию выход снизу
            };
        }


        private List<Point> RouteLoopbackPath(Point start, Point end)
        {
            var points = new List<Point> { start };

            double minLeft = Math.Min(start.X, end.X) - SettingsWindow.AppSettings.GridStep * 1.0;
            double currentLeft = SnapToGrid(minLeft);

            points.Add(new Point(currentLeft, start.Y));
            points.Add(new Point(currentLeft, end.Y));
            points.Add(end);
            return points;
        }

        private List<Point> RouteRightBypassPath(Point start, Point end)
        {
            var points = new List<Point> { start };

            double maxRight = Math.Max(start.X, end.X) + SettingsWindow.AppSettings.GridStep * 1.0;
            double currentRight = SnapToGrid(maxRight);

            points.Add(new Point(currentRight, start.Y));
            points.Add(new Point(currentRight, end.Y));
            points.Add(end);
            return points;
        }

        private bool ShouldUseHorizontalFirst(Point start, Point end, double deltaX, double deltaY)
        {
            double absDeltaX = Math.Abs(deltaX);
            double absDeltaY = Math.Abs(deltaY);

            if (absDeltaX > absDeltaY * 1.5) return true;
            if (absDeltaY > absDeltaX * 1.5) return false;

            bool horizontalClear = !IsPathBlocked(start, new Point(end.X, start.Y));
            bool verticalClear = !IsPathBlocked(start, new Point(start.X, end.Y));

            if (horizontalClear && !verticalClear) return true;
            if (verticalClear && !horizontalClear) return false;

            return absDeltaX >= absDeltaY;
        }

        private List<Point> TryMultipleRoutingStrategies(Point start, Point end, double deltaX, double deltaY, bool horizontalFirst)
        {
            var candidates = new List<(List<Point> route, int score)>();

            // Центр
            var centerRoute = horizontalFirst
                ? BuildHVHRoute(start, end, deltaX / 2.0, deltaY)
                : BuildVHVRoute(start, end, deltaX, deltaY / 2.0);
            candidates.Add((centerRoute, ScoreRoute(centerRoute)));

            // Ранний поворот
            var earlyRoute = horizontalFirst
                ? BuildHVHRoute(start, end, deltaX * 0.30, deltaY)
                : BuildVHVRoute(start, end, deltaX, deltaY * 0.40);
            candidates.Add((earlyRoute, ScoreRoute(earlyRoute)));

            // Поздний поворот
            var lateRoute = horizontalFirst
                 ? BuildHVHRoute(start, end, deltaX * 0.70, deltaY)
                 : BuildVHVRoute(start, end, deltaX, deltaY);
            candidates.Add((lateRoute, ScoreRoute(lateRoute)));

            // широкий обход
            if (candidates.All(c => c.score < 0))
            {
                double dirX = deltaX >= 0 ? 1.0 : -1.0;
                double dirY = deltaY >= 0 ? 1.0 : -1.0;
                var bypassRoute = BuildWideBypassRoute(start, end, dirX, dirY, horizontalFirst);
                candidates.Add((bypassRoute, ScoreRoute(bypassRoute)));
            }

            return candidates.OrderByDescending(c => c.score).First().route;
        }

        private int ScoreRoute(List<Point> route)
        {
            int score = 1000;
            if (IsRouteBlocked(route)) score -= 10000;
            score -= (route.Count - 2) * 10;

            double totalLength = 0;
            for (int i = 0; i < route.Count - 1; i++) totalLength += Distance(route[i], route[i + 1]);
            score -= (int)(totalLength / 10);

            int rightAngles = 0;
            for (int i = 1; i < route.Count - 1; i++)
            {
                if (IsRightAngle(route[i - 1], route[i], route[i + 1])) rightAngles++;
            }
            score += rightAngles * 5;

            return score;
        }

        private bool IsRightAngle(Point p1, Point p2, Point p3)
        {
            double dx1 = p2.X - p1.X;
            double dy1 = p2.Y - p1.Y;
            double dx2 = p3.X - p2.X;
            double dy2 = p3.Y - p2.Y;
            return Math.Abs(dx1 * dx2 + dy1 * dy2) < 1.0;
        }

        private List<Point> BuildHVHRoute(Point start, Point end, double offsetX, double deltaY)
        {
            var points = new List<Point> { start };
            double midX = SnapToGrid(start.X + offsetX);

            if (Math.Abs(midX - start.X) < SettingsWindow.AppSettings.MinSegmentLength) midX = SnapToGrid(start.X + Math.Sign(offsetX) * SettingsWindow.AppSettings.MinSegmentLength);
            if (Math.Abs(end.X - midX) < SettingsWindow.AppSettings.MinSegmentLength) midX = SnapToGrid(end.X - Math.Sign(offsetX) * SettingsWindow.AppSettings.MinSegmentLength);

            points.Add(new Point(midX, start.Y));
            points.Add(new Point(midX, end.Y));
            points.Add(end);
            return points;
        }

        private List<Point> BuildHVHVRoute(Point start, Point end, double offsetX, double deltaY)
        {
            var points = new List<Point> { start };
            double midX = SnapToGrid(start.X + offsetX);

            if (Math.Abs(midX - start.X) < SettingsWindow.AppSettings.MinSegmentLength) midX = SnapToGrid(start.X + Math.Sign(offsetX) * SettingsWindow.AppSettings.MinSegmentLength);
            if (Math.Abs(end.X - midX) < SettingsWindow.AppSettings.MinSegmentLength) midX = SnapToGrid(end.X - Math.Sign(offsetX) * SettingsWindow.AppSettings.MinSegmentLength);

            points.Add(new Point(midX, start.Y));
            points.Add(new Point(midX, end.Y));
            points.Add(end);
            points.Add(new Point(midX, start.Y));
            return points;
        }

        private List<Point> BuildVHVRoute(Point start, Point end, double deltaX, double offsetY)
        {
            var points = new List<Point> { start };
            double midY = SnapToGrid(start.Y + offsetY);

            if (Math.Abs(midY - start.Y) < SettingsWindow.AppSettings.MinSegmentLength) midY = SnapToGrid(start.Y + Math.Sign(offsetY) * SettingsWindow.AppSettings.MinSegmentLength);
            if (Math.Abs(end.Y - midY) < SettingsWindow.AppSettings.MinSegmentLength) midY = SnapToGrid(end.Y - Math.Sign(offsetY) * SettingsWindow.AppSettings.MinSegmentLength);

            points.Add(new Point(start.X, midY));
            points.Add(new Point(end.X, midY));
            points.Add(end);
            return points;
        }

        private List<Point> BuildWideBypassRoute(Point start, Point end, double dirX, double dirY, bool horizontalFirst)
        {
            var points = new List<Point> { start };
            double clearanceMultiplier = 0.5;

            if (horizontalFirst)
            {
                double offsetX = SnapToGrid(start.X + dirX * SettingsWindow.AppSettings.GridStep * clearanceMultiplier);
                if (Math.Abs(offsetX - end.X) < SettingsWindow.AppSettings.GridStep) offsetX = SnapToGrid(end.X + dirX * SettingsWindow.AppSettings.GridStep * clearanceMultiplier);

                points.Add(new Point(offsetX, start.Y));
                double offsetY = SnapToGrid(end.Y - dirY * SettingsWindow.AppSettings.GridStep);
                points.Add(new Point(offsetX, offsetY));
                points.Add(new Point(end.X, offsetY));
                points.Add(end);
            }
            else
            {
                double offsetY = SnapToGrid(start.Y + dirY * SettingsWindow.AppSettings.GridStep * clearanceMultiplier);
                if (Math.Abs(offsetY - end.Y) < SettingsWindow.AppSettings.GridStep) offsetY = SnapToGrid(end.Y + dirY * SettingsWindow.AppSettings.GridStep * clearanceMultiplier);

                points.Add(new Point(start.X, offsetY));
                double offsetX = SnapToGrid(end.X - dirX * SettingsWindow.AppSettings.GridStep);
                points.Add(new Point(offsetX, offsetY));
                points.Add(new Point(offsetX, end.Y));
                points.Add(end);
            }

            return points;
        }

        private bool IsPathBlocked(Point start, Point end)
        {
            double clearance = OBSTACLE_CLEARANCE * 1.5;
            var rect = new Rect(
                Math.Min(start.X, end.X) - clearance,
                Math.Min(start.Y, end.Y) - clearance,
                Math.Abs(end.X - start.X) + clearance * 2,
                Math.Abs(end.Y - start.Y) + clearance * 2
            );

            foreach (var child in BlocksCanvas.Children)
            {
                if (child is Border border && border.Tag is BlockItem block)
                {
                    var blockRect = new Rect(
                        Canvas.GetLeft(border) - clearance / 2,
                        Canvas.GetTop(border) - clearance / 2,
                        border.Width + clearance,
                        border.Height + clearance
                    );

                    if (DoRectsIntersect(rect, blockRect)) return true;
                }
            }
            return false;
        }

        private bool IsRouteBlocked(List<Point> route)
        {
            for (int i = 0; i < route.Count - 1; i++)
            {
                if (IsPathBlocked(route[i], route[i + 1])) return true;
            }
            return false;
        }

        private bool DoRectsIntersect(Rect r1, Rect r2)
        {
            return !(r1.Right < r2.Left || r1.Left > r2.Right || r1.Bottom < r2.Top || r1.Top > r2.Bottom);
        }

        private bool CheckPathBlockIntersectionn(List<Point> pathPoints, BlockItem fromBlock, BlockItem toBlock)
        {
            if (pathPoints == null || pathPoints.Count < 2)
                return false;

            double clearance = OBSTACLE_CLEARANCE;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Point start = pathPoints[i];
                Point end = pathPoints[i + 1];

                var segmentRect = new Rect(
                    Math.Min(start.X, end.X) - clearance,
                    Math.Min(start.Y, end.Y) - clearance,
                    Math.Abs(end.X - start.X) + clearance * 2,
                    Math.Abs(end.Y - start.Y) + clearance * 2
                );

                foreach (var child in BlocksCanvas.Children)
                {
                    if (child is Border border && border.Tag is BlockItem block)
                    {
                        // Пропускаем блоки, которые соединены этой линией
                        if (block == fromBlock || block == toBlock)
                            continue;

                        var blockRect = new Rect(
                            Canvas.GetLeft(border) - clearance / 2,
                            Canvas.GetTop(border) - clearance / 2,
                            border.Width + clearance,
                            border.Height + clearance
                        );

                        if (DoRectsIntersect(segmentRect, blockRect))
                            return true;
                    }
                }
            }

            return false;
        }

        private bool CheckPathLineOverlapp(List<Point> pathPoints, ConnectionLine currentLine)
        {
            if (pathPoints == null || pathPoints.Count < 2)
                return false;

            double clearance = OBSTACLE_CLEARANCE * 0.8;

            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Point p1 = pathPoints[i];
                Point p2 = pathPoints[i + 1];

                var currentSegmentRect = new Rect(
                    Math.Min(p1.X, p2.X) - clearance,
                    Math.Min(p1.Y, p2.Y) - clearance,
                    Math.Abs(p2.X - p1.X) + clearance * 2,
                    Math.Abs(p2.Y - p1.Y) + clearance * 2
                );

                foreach (var otherLine in connectionLines)
                {
                    // Пропускаем ту же самую линию
                    if (otherLine == currentLine)
                        continue;

                    if (otherLine.Points == null || otherLine.Points.Count < 2)
                        continue;

                    for (int j = 0; j < otherLine.Points.Count - 1; j++)
                    {
                        Point op1 = otherLine.Points[j];
                        Point op2 = otherLine.Points[j + 1];

                        var otherSegmentRect = new Rect(
                            Math.Min(op1.X, op2.X) - clearance,
                            Math.Min(op1.Y, op2.Y) - clearance,
                            Math.Abs(op2.X - op1.X) + clearance * 2,
                            Math.Abs(op2.Y - op1.Y) + clearance * 2
                        );

                        if (DoRectsIntersect(currentSegmentRect, otherSegmentRect))
                        {
                            // Проверяем, действительно ли сегменты пересекаются
                            if (DoLineSegmentsIntersect(p1, p2, op1, op2))
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool DoLineSegmentsIntersect(Point p1, Point p2, Point q1, Point q2)
        {
            double orientation1 = Orientationn(p1, p2, q1);
            double orientation2 = Orientationn(p1, p2, q2);
            double orientation3 = Orientationn(q1, q2, p1);
            double orientation4 = Orientationn(q1, q2, p2);

            // Общий случай пересечения
            if (orientation1 != orientation2 && orientation3 != orientation4)
                return true;

            // Особые случаи - коллинеарные точки
            if (orientation1 == 0 && OnSegment(p1, q1, p2)) return true;
            if (orientation2 == 0 && OnSegment(p1, q2, p2)) return true;
            if (orientation3 == 0 && OnSegment(q1, p1, q2)) return true;
            if (orientation4 == 0 && OnSegment(q1, p2, q2)) return true;

            return false;
        }

        private double Orientationn(Point p, Point q, Point r)
        {
            double val = (q.Y - p.Y) * (r.X - q.X) - (q.X - p.X) * (r.Y - q.Y);
            if (Math.Abs(val) < 0.001) return 0; // Коллинеарны
            return (val > 0) ? 1 : 2; // По часовой или против часовой
        }

        private bool OnSegment(Point p, Point q, Point r)
        {
            return q.X <= Math.Max(p.X, r.X) && q.X >= Math.Min(p.X, r.X) &&
                   q.Y <= Math.Max(p.Y, r.Y) && q.Y >= Math.Min(p.Y, r.Y);
        }

        private void UpdateConnectionLines(BlockItem movedBlock)
        {
            var relatedLines = connectionLines
                .Where(cl => cl.FromBlock == movedBlock || cl.ToBlock == movedBlock)
                .ToList();

            foreach (var connection in relatedLines)
            {
                Point start = GetAnchorPosition(connection.FromBlock, connection.Type, isOutput: true);

                ConnectionType routingType = connection.Type;
                ConnectionType targetAnchorType = ConnectionType.Input;

                if ((connection.FromBlock.Type == BlockType.LoopConnector || connection.FromBlock.Type == BlockType.DoLoopConnector) &&
                   (connection.ToBlock.Type == BlockType.While ||
                    connection.ToBlock.Type == BlockType.DoWhile ||
                    connection.ToBlock.Type == BlockType.For))
                {
                    targetAnchorType = ConnectionType.LoopBody;
                    routingType = ConnectionType.LoopBody;
                }

                Point end = GetAnchorPosition(connection.ToBlock, targetAnchorType, isOutput: false);
                var newPoints = RoutePath(start, end, routingType, connection.FromBlock);

                if (connection.VisualPath == null)
                {
                    connection.VisualPath = new Polyline { StrokeThickness = 3 };
                    FlowchartCanvas.Children.Add(connection.VisualPath);
                }

                var pc = new PointCollection();
                foreach (var p in newPoints) pc.Add(p);
                connection.VisualPath.Points = pc;
                connection.Points = newPoints;

                var baseColor = connection.Type switch
                {
                    ConnectionType.TrueBranch => Colors.LimeGreen,
                    ConnectionType.FalseBranch => Colors.IndianRed,
                    ConnectionType.LoopBody => Colors.DeepSkyBlue,
                    _ => Colors.White
                };

                bool intersectsBlock = CheckPathBlockIntersectionn(newPoints, connection.FromBlock, connection.ToBlock);
                bool overlapsLine = CheckPathLineOverlapp(newPoints, connection);

                connection.VisualPath.Stroke = (intersectsBlock || overlapsLine)
                    ? ErrorLineColor
                    : new SolidColorBrush(baseColor);

                if (connection.VisualLabel != null)
                {
                    BlocksCanvas.Children.Remove(connection.VisualLabel);
                    connection.VisualLabel = null;
                }

                if (connection.Type == ConnectionType.TrueBranch || connection.Type == ConnectionType.FalseBranch)
                {
                    string labelText = connection.Type == ConnectionType.TrueBranch ? "Да" : "Нет";

                    var label = new TextBlock
                    {
                        Text = labelText,
                        Foreground = connection.VisualPath.Stroke,
                        FontSize = 14,
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                        IsHitTestVisible = false
                    };

                    if (connection.Points != null && connection.Points.Count > 0)
                    {
                        Point anchorPoint = connection.Points[0];
                        const double TEXT_PADDING = 5.0;
                        const double TEXT_HEIGHT_OFFSET = 20.0;

                        double offsetX = (connection.Type == ConnectionType.TrueBranch)
                            ? TEXT_PADDING
                            : -(25.0 + TEXT_PADDING);

                        if (connection.Type == ConnectionType.FalseBranch && connection.FromBlock.Type == BlockType.While)
                        {
                            offsetX = TEXT_PADDING;
                        }

                        Canvas.SetLeft(label, anchorPoint.X + offsetX);
                        Canvas.SetTop(label, anchorPoint.Y - TEXT_HEIGHT_OFFSET);

                        // BlocksCanvas.Children.Add(label);
                        connection.VisualLabel = label;
                    }
                }

                if (connection.ArrowHead != null)
                {
                    FlowchartCanvas.Children.Remove(connection.ArrowHead);
                }

                var arrowColor = (connection.VisualPath.Stroke as SolidColorBrush)?.Color ?? Colors.White;
                connection.ArrowHead = CreateArrowHeadForPath(newPoints, arrowColor);
                if (connection.ArrowHead != null)
                {
                    FlowchartCanvas.Children.Add(connection.ArrowHead);
                }
            }
        }

        private void CreateManualConnection(BlockItem from, BlockItem to, ConnectionType type)
        {
            SaveState();
            if (from == to) return;
            if (connectionLines.Any(cl => cl.FromBlock == from && cl.ToBlock == to && cl.Type == type)) return;

            Point startAnchor = GetAnchorPosition(from, type, isOutput: true);

            ConnectionType endType = ConnectionType.Input;
            ConnectionType routingType = type;
            if (from.Type == BlockType.LoopConnector && to.Type == BlockType.While && type == ConnectionType.Normal)
            {
                endType = ConnectionType.LoopBody;
                routingType = ConnectionType.LoopBody;
            }
            else if (to.Type == BlockType.DoLoopConnector && (from.Type == BlockType.While || from.Type == BlockType.DoWhile) && type == ConnectionType.LoopBody)
            {
                endType = ConnectionType.LoopBody;
                routingType = ConnectionType.LoopBody;
            }
            Point endAnchor = GetAnchorPosition(to, endType, isOutput: false);
            var points = RoutePath(startAnchor, endAnchor, routingType, from);

            var baseColor = type switch
            {
                ConnectionType.TrueBranch => Colors.LimeGreen,
                ConnectionType.FalseBranch => Colors.IndianRed,
                ConnectionType.LoopBody => Colors.DeepSkyBlue,
                _ => Colors.White
            };
            var brush = new SolidColorBrush(baseColor);

            var polyline = new Polyline { Stroke = brush, StrokeThickness = 2, Points = new PointCollection(), StrokeLineJoin = PenLineJoin.Round };
            foreach (var p in points) polyline.Points.Add(p);
            polyline.RightTapped += Polyline_RightTapped;
            FlowchartCanvas.Children.Add(polyline);

            var arrow = CreateArrowHeadForPath(points, baseColor);
            FlowchartCanvas.Children.Add(arrow);

            var connection = new ConnectionLine
            {
                FromBlock = from,
                ToBlock = to,
                Type = type,
                VisualPath = polyline,
                ArrowHead = arrow,
                Points = points,
                Stroke = brush
            };

            // Проверяем пересечения сразу при создании
            bool intersectsBlock = CheckPathBlockIntersectionn(points, from, to);
            bool overlapsLine = CheckPathLineOverlapp(points, connection);

            if (intersectsBlock || overlapsLine)
            {
                polyline.Stroke = ErrorLineColor;
                arrow.Fill = ErrorLineColor;
                connection.Stroke = ErrorLineColor;
            }

            if (type == ConnectionType.TrueBranch || type == ConnectionType.FalseBranch)
            {
                string txt = type == ConnectionType.TrueBranch ? "Да" : "Нет";
                var label = new TextBlock
                {
                    Text = txt,
                    Foreground = polyline.Stroke,
                    FontSize = 14,
                    FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                    IsHitTestVisible = false
                };

                if (points.Count > 0)
                {
                    Point p0 = points[0];
                    double offX = (type == ConnectionType.TrueBranch) ? 8.0 : -33.0;

                    if (type == ConnectionType.FalseBranch && from.Type == BlockType.While)
                        offX = 8.0;

                    Canvas.SetLeft(label, p0.X + offX);
                    Canvas.SetTop(label, p0.Y - 18.0);

                    // BlocksCanvas.Children.Add(label);
                    connection.VisualLabel = label;
                }
            }

            connectionLines.Add(connection);
            BuildSyntaxTree();
        }

        private Polygon CreateArrowHeadForPath(IList<Point> points, Windows.UI.Color color)
        {
            if (points == null || points.Count < 2) return null;
            var end = points[points.Count - 1];
            var prev = points[points.Count - 2];
            double angle = Math.Atan2(end.Y - prev.Y, end.X - prev.X) * 180.0 / Math.PI;
            var arrow = new Polygon
            {
                Points = new PointCollection { new Point(0, 0), new Point(-12, -6), new Point(-12, 6) },
                Fill = new SolidColorBrush(color),
                RenderTransform = new RotateTransform { Angle = angle }
            };
            Canvas.SetLeft(arrow, end.X);
            Canvas.SetTop(arrow, end.Y);
            return arrow;
        }

        private Point GetAnchorPosition(BlockItem block, ConnectionType type, bool isOutput)
        {
            double left = SnapToGrid(block.CanvasLeft);
            double top = SnapToGrid(block.CanvasTop);
            double width = 100;
            double height = 60;

            if (!isOutput)
            {
                if (type == ConnectionType.LoopBody)
                {
                    if (block.Type == BlockType.While || block.Type == BlockType.DoWhile || block.Type == BlockType.For || block.Type == BlockType.DoLoopConnector)
                    {
                        return new Point(left, top + height * 0.5); // Левый якорь
                    }
                }
                return new Point(left + width * 0.5, top); // Вход сверху по умолчанию
            }

            return block.Type switch
            {
                BlockType.While => type switch
                {
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5), // Выход слева
                    ConnectionType.TrueBranch => new Point(left + width * 0.5, top + height), // Выход снизу
                    ConnectionType.FalseBranch => new Point(left + width, top + height * 0.5), // Выход справа
                    _ => new Point(left + width * 0.5, top + height) // По умолчанию снизу
                },
                BlockType.DoWhile => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5), // Выход справа
                    ConnectionType.FalseBranch => new Point(left + width * 0.5, top + height), // Выход снизу
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5), // Выход слева
                    _ => new Point(left + width * 0.5, top + height) // По умолчанию снизу
                },
                BlockType.For => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width * 0.5, top + height), // Выход снизу
                    ConnectionType.FalseBranch => new Point(left + width * 1.0, top + height * 0.5), // Выход справа
                    _ => new Point(left + width * 0.5, top + height) // По умолчанию снизу
                },
                BlockType.LoopConnector => new Point(left, top + height * 0.5), // Выход слева

                BlockType.DoLoopConnector => type switch
                {
                    ConnectionType.Normal => new Point(left + width * 0.5, top + height), // Выход снизу
                    ConnectionType.LoopBody => new Point(left, top + height * 0.5), // Выход слева
                    _ => new Point(left + width * 0.5, top + height) // По умолчанию снизу
                },

                BlockType.Decision => type switch
                {
                    ConnectionType.TrueBranch => new Point(left + width, top + height * 0.5), // Выход справа
                    ConnectionType.FalseBranch => new Point(left, top + height * 0.5), // Выход слева
                    _ => new Point(left + width, top + height * 0.5) // По умолчанию справа
                },

                BlockType.Start => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.Process => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.Input => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.Output => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.InputOutput => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.VariableDeclaration => new Point(left + width * 0.5, top + height), // Выход снизу
                BlockType.ArrayDeclaration => new Point(left + width * 0.5, top + height), // Выход снизу

                _ => new Point(left + width * 0.5, top + height) // По умолчанию выход снизу
            };
        }

        private Ellipse FindNearestAvailableAnchor(Point position, double threshold)
        {
            foreach (var blockItem in listofblocks)
            {
                if (blockItem == connectionStartBlock) continue;

                var blockBorder = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == blockItem);
                if (blockBorder?.Child is Grid grid)
                {
                    foreach (var child in grid.Children)
                    {
                        if (child is Border hitBox && hitBox.Child is Ellipse anchor)
                        {
                            if (hitBox.Tag is ValueTuple<BlockItem, ConnectionType> tag && tag.Item2 == ConnectionType.Normal)
                            {
                                Point anchorCenter = GetAnchorPosition(blockItem, ConnectionType.Input, isOutput: false);
                                if (Distance(position, anchorCenter) < threshold) return anchor;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private GridNode FindNearestFreeNodeWithoutCollisions(BlockItem block, int startRow, int startCol)
        {
            int rStart = Math.Max(0, Math.Min(GRID_ROWS - 1, startRow));
            int cStart = Math.Max(0, Math.Min(GRID_COLUMNS - 1, startCol));

            var visited = new bool[GRID_ROWS, GRID_COLUMNS];
            var q = new Queue<(int r, int c)>();
            q.Enqueue((rStart, cStart));
            visited[rStart, cStart] = true;

            (int r, int c)[] dirs = new (int, int)[] { (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) };

            while (q.Count > 0)
            {
                var (r, c) = q.Dequeue();
                GridNode currentNode = virtualGrid[r, c];

                if (currentNode.IsAvailable)
                {
                    block.CanvasLeft = currentNode.Column * SettingsWindow.AppSettings.GridStep;
                    block.CanvasTop = currentNode.Row * SettingsWindow.AppSettings.GridStep;

                    if (!CheckCollisionAtTemporaryLocation(block)) return currentNode;
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
            return null;
        }

        private bool CheckCollisionAtTemporaryLocation(BlockItem block)
        {
            var border = BlocksCanvas.Children.OfType<Border>().FirstOrDefault(b => b.Tag == block);
            double originalLeft = -1, originalTop = -1;
            if (border != null)
            {
                originalLeft = Canvas.GetLeft(border);
                originalTop = Canvas.GetTop(border);
                Canvas.SetLeft(border, block.CanvasLeft);
                Canvas.SetTop(border, block.CanvasTop);
            }

            bool collisionDetected = false;
            var relatedLines = connectionLines.Where(cl => cl.FromBlock == block || cl.ToBlock == block).ToList();

            foreach (var connection in relatedLines)
            {
                Point start = GetAnchorPosition(connection.FromBlock, connection.Type, isOutput: true);

                ConnectionType targetType = ConnectionType.Input;
                if ((connection.FromBlock.Type == BlockType.LoopConnector || connection.FromBlock.Type == BlockType.DoLoopConnector) &&
                   (connection.ToBlock.Type == BlockType.While || connection.ToBlock.Type == BlockType.DoWhile || connection.ToBlock.Type == BlockType.For))
                {
                    targetType = ConnectionType.LoopBody;
                }

                Point end = GetAnchorPosition(connection.ToBlock, targetType, isOutput: false);
                var pathPoints = RoutePath(start, end);

                if (CheckPathBlockIntersectionn(pathPoints, connection.FromBlock, connection.ToBlock) ||
                    CheckPathLineOverlapp(pathPoints, connection))
                {
                    collisionDetected = true;
                    break;
                }
            }

            if (border != null)
            {
                Canvas.SetLeft(border, originalLeft);
                Canvas.SetTop(border, originalTop);
            }
            return collisionDetected;
        }

        private GridNode FindFirstFreeGridNodeInViewport()
        {
            double viewportLeft = MainScrollViewer.HorizontalOffset;
            double viewportTop = MainScrollViewer.VerticalOffset;
            double viewportRight = viewportLeft + MainScrollViewer.ViewportWidth;
            double viewportBottom = viewportTop + MainScrollViewer.ViewportHeight;

            int startCol = Math.Max(0, (int)(viewportLeft / SettingsWindow.AppSettings.GridStep));
            int endCol = Math.Min(GRID_COLUMNS - 1, (int)(viewportRight / SettingsWindow.AppSettings.GridStep));
            int startRow = Math.Max(0, (int)(viewportTop / SettingsWindow.AppSettings.GridStep));
            int endRow = Math.Min(GRID_ROWS - 1, (int)(viewportBottom / SettingsWindow.AppSettings.GridStep));

            if (!listofblocks.Any())
            {
                int centerRow = (startRow + endRow) / 2;
                int centerCol = (startCol + endCol) / 2;
                if (virtualGrid[centerRow, centerCol].IsAvailable) return virtualGrid[centerRow, centerCol];
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

            (int r, int c)[] dirs = new (int, int)[] { (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) };

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
            int rStart = Math.Max(0, Math.Min(GRID_ROWS - 1, startRow));
            int cStart = Math.Max(0, Math.Min(GRID_COLUMNS - 1, startCol));
            var visited = new bool[GRID_ROWS, GRID_COLUMNS];
            var q = new Queue<(int r, int c)>();
            q.Enqueue((rStart, cStart));
            visited[rStart, cStart] = true;
            (int r, int c)[] dirs = new (int, int)[] { (0, 1), (1, 0), (0, -1), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1) };

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

        private bool IsLineSegmentCloseToRect(Point p1, Point p2, Rect rect, double threshold)
        {
            double x_min = Math.Min(p1.X, p2.X) - threshold;
            double x_max = Math.Max(p1.X, p2.X) + threshold;
            double y_min = Math.Min(p1.Y, p2.Y) - threshold;
            double y_max = Math.Max(p1.Y, p2.Y) + threshold;

            if (rect.Right < x_min || rect.Left > x_max || rect.Bottom < y_min || rect.Top > y_max) return false;

            if (Math.Abs(p1.X - p2.X) < 10) return rect.Left <= p1.X + threshold && rect.Right >= p1.X - threshold;
            else if (Math.Abs(p1.Y - p2.Y) < 10) return rect.Top <= p1.Y + threshold && rect.Bottom >= p1.Y - threshold;

            return false;
        }
    }
}
