using Blocks_.Core.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Blocks_.haru
{
    public static class DrawBlock
    {
        public static Border GetBlock(BlockItem item)
        {
            if (item == null)
                return null;

            var grid = new Grid
            {
                Width = 100,
                Height = 60
            };

            Shape shape;

            switch (item.Type)
            {
                case BlockType.Start:
                case BlockType.End:
                    double w = grid.Width;
                    double h = grid.Height;
                    double r = h / 2;

                    shape = new Path
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Data = new PathGeometry
                        {
                            Figures = new PathFigureCollection
                            {
                                new PathFigure
                                {
                                    StartPoint = new Point(r, 0),
                                    IsClosed = true,
                                    Segments = new PathSegmentCollection
                                    {
                                        new LineSegment { Point = new Point(w - r, 0) },
                                        new ArcSegment
                                        {
                                            Point = new Point(w - r, h),
                                            Size = new Size(r, r),
                                            SweepDirection = SweepDirection.Clockwise
                                        },
                                        new LineSegment { Point = new Point(r, h) },
                                        new ArcSegment
                                        {
                                            Point = new Point(r, 0),
                                            Size = new Size(r, r),
                                            SweepDirection = SweepDirection.Clockwise
                                        }
                                    }
                                }
                            }
                        }
                    };
                    break;

                case BlockType.Decision:
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(grid.Width/2, 0),
                            new Point(grid.Width, grid.Height/2),
                            new Point(grid.Width/2, grid.Height),
                            new Point(0, grid.Height/2)
                        }
                    };
                    break;
                case BlockType.While: // Ромб для условия цикла (while)
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = item.BorderColor,
                        StrokeThickness = 2,
                        Points = new PointCollection
                {
                    new Point(grid.Width / 2, 0),
                    new Point(grid.Width, grid.Height / 2),
                    new Point(grid.Width / 2, grid.Height),
                    new Point(0, grid.Height / 2)
                }
                    };
                    break;

                    break;
                case BlockType.DoWhile: // Ромб для условия цикла (while)
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = item.BorderColor,
                        StrokeThickness = 2,
                        Points = new PointCollection
                {
                    new Point(grid.Width / 2, 0),
                    new Point(grid.Width, grid.Height / 2),
                    new Point(grid.Width / 2, grid.Height),
                    new Point(0, grid.Height / 2)
                }
                    };
                    break;

                case BlockType.LoopConnector: // НОВОЕ: Круг-соединитель
                                              // Уменьшаем размер сетки для круга
                    shape = new Ellipse
                    {
                        Width = grid.Width / 3,
                        Height = grid.Height / 2,
                        Fill = item.BackgroundColor, // Белый
                        Stroke = new SolidColorBrush(Colors.Black), // Черная граница
                        StrokeThickness = 2
                    };
                    break;
                case BlockType.For:
                    // Шестиугольник для цикла
                    double indent = 15;
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(indent, 0),
                            new Point(grid.Width - indent, 0),
                            new Point(grid.Width, grid.Height/2),
                            new Point(grid.Width - indent, grid.Height),
                            new Point(indent, grid.Height),
                            new Point(0, grid.Height/2)
                        }
                    };
                    break;
                case BlockType.InputOutput:
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(20, 0),
                            new Point(grid.Width, 0),
                            new Point(grid.Width - 20, grid.Height),
                            new Point(0, grid.Height)
                        }
                    };
                    break;

                case BlockType.VariableDeclaration: // <--- ДОБАВЛЕНО
                    shape = new Rectangle
                    {
                        Fill = item.BackgroundColor,
                        Stroke = item.BorderColor,
                        StrokeThickness = 2,
                        RadiusX = 5,
                        RadiusY = 5
                    };
                    break;

                case BlockType.ArrayDeclaration: // НОВЫЙ ТИП - Массивы
                    // Создаем двойной прямоугольник для визуального отличия
                    var outerRect = new Rectangle
                    {
                        Fill = item.BackgroundColor,
                        Stroke = item.BorderColor,
                        StrokeThickness = 2,
                        RadiusX = 5,
                        RadiusY = 5
                    };

                    var innerRect = new Rectangle
                    {
                        Fill = new SolidColorBrush(Colors.Transparent),
                        Stroke = item.BorderColor,
                        StrokeThickness = 1,
                        RadiusX = 3,
                        RadiusY = 3,
                        Width = grid.Width - 10,
                        Height = grid.Height - 10
                    };

                    grid.Children.Add(outerRect);
                    shape = innerRect;
                    break;

                case BlockType.Input:
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(20, 0),
                            new Point(grid.Width, 0),
                            new Point(grid.Width - 20, grid.Height),
                            new Point(0, grid.Height)
                        }
                    };
                    break;
                case BlockType.Output:
                    shape = new Polygon
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2,
                        Points = new PointCollection
                        {
                            new Point(20, 0),
                            new Point(grid.Width, 0),
                            new Point(grid.Width - 20, grid.Height),
                            new Point(0, grid.Height)
                        }
                    };
                    break;

                case BlockType.Process:
                default:
                    shape = new Rectangle
                    {
                        Fill = item.BackgroundColor,
                        Stroke = new SolidColorBrush(Colors.White),
                        StrokeThickness = 2
                    };
                    break;
            }

            // Текст с кодом блока или названием
            var textBlock = new TextBlock
            {
                Text = item.Name,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                MaxWidth = 90,
                Margin = new Thickness(5)
            };

            var codeBlock = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(item.Code) ? " " : item.Code,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,
                MaxWidth = 90,
                Margin = new Thickness(5)
            };

            grid.Children.Add(shape);
            grid.Children.Add(textBlock);
            grid.Children.Add(codeBlock);

            // Создаём якоря для соединений
            var anchors = CreateAnchors(item, grid);
            foreach (var a in anchors)
                grid.Children.Add(a);

            var border = new Border
            {
                Width = grid.Width,
                Height = grid.Height,
                Child = grid,
                Tag = item
            };

            return border;
        }

        private static List<FrameworkElement> CreateAnchors(BlockItem item, Grid grid)
        {
            var list = new List<FrameworkElement>();

            if (item.Type != BlockType.Start)
            {
                list.Add(CreateAnchor(item, ConnectionType.Input, new Point(0.5, 0), Colors.SkyBlue));
            }

            if (item.Type == BlockType.Decision)
            {
                list.Add(CreateAnchor(item, ConnectionType.TrueBranch, new Point(1.0, 0.5), Colors.LimeGreen));
                list.Add(CreateAnchor(item, ConnectionType.FalseBranch, new Point(0.0, 0.5), Colors.IndianRed));
                list.Add(CreateAnchor(item, ConnectionType.Normal, new Point(0.5, 1.0), Colors.White));
            }

            // Конфигурация якорей в зависимости от типа блока
            if (item.Type == BlockType.While)
            {
                // ЦИКЛ WHILE (ромб):
                // - Левый: принимает обратную связь от тела (LoopBody - вход тела)
                // - Правый: выход из цикла (FalseBranch - "Нет")
                // - Нижний: выход к телу цикла (LoopBody - "Да", к соединителю)

                list.Add(CreateAnchor(item, ConnectionType.LoopBody, new Point(0.0, 0.5), Colors.LimeGreen)); // Левый - вход от тела
                list.Add(CreateAnchor(item, ConnectionType.FalseBranch, new Point(1.0, 0.5), Colors.IndianRed)); // Правый - выход из цикла
                list.Add(CreateAnchor(item, ConnectionType.TrueBranch, new Point(0.5, 1.0), Colors.DeepSkyBlue)); // Нижний - к телу цикла
            }
            else if (item.Type == BlockType.LoopConnector)
            {
                // СОЕДИНИТЕЛЬ (круг - тело цикла):
                // - Верхний: вход (уже есть как Input)
                // - Левый: выход обратно к условию цикла

                list.Add(CreateAnchor(item, ConnectionType.Normal, new Point(0.0, 0.5), Colors.White)); // Левый - обратно к циклу
            }
            else if (item.Type == BlockType.Loop)
            {
                list.Add(CreateAnchor(item, ConnectionType.LoopBody, new Point(1.0, 0.5), Colors.DeepSkyBlue));
                list.Add(CreateAnchor(item, ConnectionType.Normal, new Point(0.5, 1.0), Colors.White));
            }
            else if (item.Type == BlockType.For)
            {
                // ЦИКЛ FOR:
                // - Левый: LoopBody = возврат с инкрементом (тело -> for)
                // - Правый: TrueBranch = переход к телу
                // - Нижний: FalseBranch = выход из цикла

                list.Add(CreateAnchor(item, ConnectionType.LoopBody, new Point(0.0, 0.5), Colors.DeepSkyBlue));  // обратная связь (step)
                list.Add(CreateAnchor(item, ConnectionType.TrueBranch, new Point(1.0, 0.5), Colors.LimeGreen)); // переход к телу
                list.Add(CreateAnchor(item, ConnectionType.FalseBranch, new Point(0.5, 1.0), Colors.IndianRed)); // выход
            }
            else if (item.Type == BlockType.DoWhile)
            {
                // DO-WHILE:
                // - Левый: LoopBody = начальный вход тела
                // - Правый: TrueBranch = повторить (к телу)
                // - Нижний: FalseBranch = выход после цикла

                list.Add(CreateAnchor(item, ConnectionType.LoopBody, new Point(0.0, 0.5), Colors.DeepSkyBlue)); // вход тела (первый запуск)
                list.Add(CreateAnchor(item, ConnectionType.TrueBranch, new Point(1.0, 0.5), Colors.LimeGreen)); // повтор
                list.Add(CreateAnchor(item, ConnectionType.FalseBranch, new Point(0.5, 1.0), Colors.IndianRed)); // выход
            }
            else if (item.Type != BlockType.End)
            {
                list.Add(CreateAnchor(item, ConnectionType.Normal, new Point(0.5, 1.0), Colors.White));
            }

            return list;
        }


        private static Border CreateAnchor(BlockItem block, ConnectionType type, Point relativePos, Windows.UI.Color color)
        {
            var anchor = new Ellipse
            {
                Width = 10,
                Height = 10,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1.5,
                Tag = (block, type),
                Opacity = 0.9
            };

            // Контейнер для расширенной зоны клика
            var hitBox = new Border
            {
                Width = 25,
                Height = 25,
                Background = new SolidColorBrush(Colors.Transparent),
                Child = anchor,
                Tag = (block, type)
            };

            // Определяем выравнивание всего hitBox (по сетке)
            if (relativePos.X < 0.25)
                hitBox.HorizontalAlignment = HorizontalAlignment.Left;
            else if (relativePos.X > 0.75)
                hitBox.HorizontalAlignment = HorizontalAlignment.Right;
            else
                hitBox.HorizontalAlignment = HorizontalAlignment.Center;

            if (relativePos.Y < 0.25)
                hitBox.VerticalAlignment = VerticalAlignment.Top;
            else if (relativePos.Y > 0.75)
                hitBox.VerticalAlignment = VerticalAlignment.Bottom;
            else
                hitBox.VerticalAlignment = VerticalAlignment.Center;

            // ⚙️ Сдвигаем сам кружок внутри hitBox, чтобы он лежал на границе
            var margin = -2.0; // расстояние от границы блока

            double offsetX = 0, offsetY = 0;
            if (relativePos.X < 0.25)
                offsetX = -hitBox.Width / 2 + anchor.Width / 2 + margin;
            else if (relativePos.X > 0.75)
                offsetX = hitBox.Width / 2 - anchor.Width / 2 - margin;

            if (relativePos.Y < 0.25)
                offsetY = -hitBox.Height / 2 + anchor.Height / 2 + margin;
            else if (relativePos.Y > 0.75)
                offsetY = hitBox.Height / 2 - anchor.Height / 2 - margin;

            anchor.RenderTransform = new TranslateTransform
            {
                X = offsetX,
                Y = offsetY
            };

            return hitBox;
        }
    }
    }