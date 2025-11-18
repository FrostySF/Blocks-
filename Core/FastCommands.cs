using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {

        /// <summary>
        /// Вычисляет расстояние между двумя точками
        /// </summary>
        private double Distance(Point p1, Point p2) => Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));

        /// <summary>
        /// Привязка координаты к виртуальной сетке
        /// </summary>
        private double SnapToGrid(double value) => Math.Round(value / GRID_STEP) * GRID_STEP;

        public Tree GetSyntaxTree() => syntaxTreeRoot;
        private async void About_Click(object sender, RoutedEventArgs e)
        {
            // Получаем версию приложения
            string version = GetAppVersion();

            var dialog = new ContentDialog
            {
                Title = "О программе",
                Content =
                    $"Блок-схема редактор\n" +
                    $"Было сделано для замены 9_14\n" +
                    $"Сделал Хару и делаю лапками!\n" +
                    $"Версия {version}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
        }

        private string GetAppVersion()
        {
            if (Package.Current != null)
            {
                var v = Package.Current.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
            else
            {
                var assembly = Assembly.GetExecutingAssembly().GetName().Version;
                return assembly != null
                    ? $"{assembly.Major}.{assembly.Minor}.{assembly.Build}.{assembly.Revision}"
                    : "1.0.0.0";
            }
        }

        private void Documentation_Click(object sender, RoutedEventArgs e)
        {
            string url = "https://chinoharu.ru/blocks/docs";

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowNotification($"Не удалось открыть ссылку: {ex.Message}");
            }
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            currentZoom += 0.1;
            ApplyZoom();
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentZoom > 0.2)
                currentZoom -= 0.1;
            ApplyZoom();
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            currentZoom = 1.0;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            MainScrollViewer.ChangeView(
                MainScrollViewer.HorizontalOffset,
                MainScrollViewer.VerticalOffset,
                (float)currentZoom);
        }

        private void InitializeBlockContextMenu(Border blockControl)
        {
            var menu = new MenuFlyout();

            var deleteItem = new MenuFlyoutItem
            {
                Text = "Удалить",
                Icon = new FontIcon { Glyph = "\xE74D" }
            };
            deleteItem.Click += (s, e) => DeleteBlock(blockControl);

            var editItem = new MenuFlyoutItem
            {
                Text = "Редактировать",
                Icon = new FontIcon { Glyph = "\xE70F" }
            };
            editItem.Click += (s, e) =>
            {
                if (blockControl.Tag is BlockItem block)
                    _ = ShowEditDialogForBlock(block);
            };

            var info = new MenuFlyoutItem
            {
                Text = "Инфо",
                Icon = new FontIcon { Glyph = "\xE70F" }
            };
            info.Click += (s, e) => ShowBlockInfo(blockControl);

            menu.Items.Add(info);
            menu.Items.Add(editItem);
            menu.Items.Add(deleteItem);

            blockControl.ContextFlyout = menu;
        }

        private void ShowBlockInfo(BlockItem block)
        {
            var dialog = new ContentDialog
            {
                Title = $"Информация о блоке: {block.Name}",
                Content = block.Description,
                PrimaryButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };

            dialog.ShowAsync();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void NewFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Новая блок-схема",
                Content = "Создать новую блок-схему?",
                PrimaryButtonText = "Создать",
                CloseButtonText = "Отмена",
                XamlRoot = this.Content.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                blockCounter = 0;
                BlocksCanvas.Children.Clear();
                listofblocks.Clear();
                ClearConnectionLines();
                InitializeVirtualGrid();
                startBlock = null;
                endBlock = null;
                HighlightAvailableCells();
            }
        }

        private async void OpenFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeFilter.Add(".xml");
            filePicker.FileTypeFilter.Add(".prg");

            StorageFile file = await filePicker.PickSingleFileAsync();
            if (file != null)
            {
                await LoadFlowchartFromFile(file);
            }
        }

        private async void SaveFlowchart_Click(object sender, RoutedEventArgs e)
        {
            var filePicker = new FileSavePicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            filePicker.FileTypeChoices.Add("Блок-схема XML", new List<string> { ".xml", ".prg" });

            StorageFile file = await filePicker.PickSaveFileAsync();
            if (file != null)
            {
                try
                {
                    var dataToSave = new FlowchartData
                    {
                        Blocks = new ObservableCollection<BlockItem>(this.listofblocks),
                        Connections = this.connectionLines
                    };
                    await XmlDataSerializer.SaveToFileAsync(dataToSave, file);

                    ShowNotification($"Блок-схема успешно сохранена в:\n{file.Path}\n\nБлоков: {listofblocks.Count}\nСоединений: {connectionLines.Count}");
                }
                catch (Exception ex)
                {
                    ShowNotification($"Не удалось сохранить блок-схему:\n{ex.Message}");
                }
            }
        }

        private void ShowBlockInfo(Border blockControl)
        {
            if (blockControl.Tag is BlockItem block)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Информация о блоке: {block.Name}",
                    Content = block.Description,
                    PrimaryButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };

                dialog.ShowAsync();
            }
        }


        public void AddBlock(string name, string icon, string description, BlockType type)
        {
            Blocks.Add(new BlockItem
            {
                Name = name,
                Icon = icon,
                Description = description,
                Type = type
            });
        }

        /// <summary>
        /// Вызывается при двойном клике на блоке. Показывает соответствующий диалог редактирования.
        /// Обновите существующий метод или добавьте эту логику в BlockControl_DoubleTapped.
        /// </summary>
        public async Task ShowEditDialogForBlock(BlockItem block)
        {
            switch (block.Type)
            {
                case BlockType.While:
                    await ShowWhileEditDialog(block);
                    break;

                case BlockType.DoWhile:
                    await ShowDoWhileEditDialog(block);
                    break;

                case BlockType.For:
                    await ShowForEditDialog(block);
                    break;

                case BlockType.Decision:
                case BlockType.Process:
                case BlockType.Input:
                case BlockType.Output:
                case BlockType.VariableDeclaration:
                case BlockType.ArrayDeclaration:
                    // Используем существующий EditWindow.Show() для этих типов
                    EditWindow.Show(block);
                    break;

                case BlockType.Start:
                case BlockType.End:
                case BlockType.LoopConnector:
                    // Эти блоки не редактируются
                    ShowNotification($"Блок '{block.Name}' не может быть отредактирован.");
                    break;

                default:
                    EditWindow.Show(block);
                    break;
            }
        }

        private void StartDebug_Click(object sender, RoutedEventArgs e)
        {
            BuildSyntaxTree();
            if (!InitializeVariables())
            {
                TraceTextBlock.Text += $"\n--- ОШИБКА ИНИЦИАЛИЗАЦИИ. ВЫПОЛНЕНИЕ ПРЕРВАНО. ---";
                return;
            }

            // PseudocodeTextBlock.Text = GenerateCodeFromTree();
            TraceTextBlock.Text = " ";
            if (syntaxTreeRoot == null)
            {
                TraceTextBlock.Text = "Нет стартового блока";
                return;
            }

            executionOrder.Clear();
            if (syntaxTreeRoot != null)
            {
                executionOrder.Add(syntaxTreeRoot);
            }

            if (executionOrder.Count == 0)
            {
                TraceTextBlock.Text = "Нет стартового блока.";
                return;
            }

            isDebugging = true;
            currentStepIndex = 0;
            currentDebugNode = executionOrder[currentStepIndex];

            HighlightCurrentBlock(currentDebugNode.Block);

        }

        private void StopDebug_Click(object sender, RoutedEventArgs e)
        {
            isDebugging = false;
            currentDebugNode = null;
            currentStepIndex = -1;
            executionOrder.Clear();
            TraceTextBlock.Text += "\nОтладка остановлена.";

            ClearBlockHighlights();
            ClearVariablePreviewPanels();
        }

        private void StepDebug_Click(object sender, RoutedEventArgs e)
        {
            if (!isDebugging || executionOrder.Count == 0)
            {
                TraceTextBlock.Text = "Отладка не запущена.";
                return;
            }

            if (currentStepIndex >= executionOrder.Count)
            {
                TraceTextBlock.Text += "\nВыполнение завершено.";

                isDebugging = false;
                currentDebugNode = null;
                currentStepIndex = -1;
                executionOrder.Clear();
                TraceTextBlock.Text += "\nОтладка остановлена.";

                ClearBlockHighlights();
                return;
            }

            currentDebugNode = executionOrder[currentStepIndex];
            HighlightCurrentBlock(currentDebugNode.Block);
            ScrollToBlock(currentDebugNode.Block);

            TraceTextBlock.Text += $"\n [{currentStepIndex + 1}] {currentDebugNode.Block.Name}";
            bool result = ExecuteBlock(currentDebugNode);
            Tree next = null;

            switch (currentDebugNode.Block.Type)
            {
                case BlockType.Decision:
                    {
                        ConnectionType branch = result switch
                        {
                            true => ConnectionType.TrueBranch,
                            false => ConnectionType.FalseBranch
                        };

                        next = currentDebugNode.Children
                            .FirstOrDefault(c => c.BranchType == branch);
                        break;
                    }



                case BlockType.Loop:
                case BlockType.For:
                case BlockType.While:
                case BlockType.DoWhile:
                    {
                        ConnectionType branch = result switch
                        {
                            true => ConnectionType.LoopBody,
                            false => ConnectionType.LoopExit
                        };

                        next = currentDebugNode.Children
                            .FirstOrDefault(c => c.BranchType == branch);
                        break;
                    }

                case BlockType.LoopConnector:
                    {
                        // Находим родительский блок цикла
                        var parent = currentDebugNode.Parent;
                        if (parent != null && (parent.Block.Type == BlockType.While || parent.Block.Type == BlockType.DoWhile || parent.Block.Type == BlockType.For))
                        {
                            next = parent; // Возврат к условию цикла или FOR-блоку
                        }
                        else
                        {
                            // Если это обычный LoopConnector без цикла, берём LoopBody
                            next = currentDebugNode.Children
                                .FirstOrDefault(c => c.BranchType == ConnectionType.LoopBody);
                        }
                        break;
                    }

                case BlockType.End:
                    next = null;
                    break;

                default:
                    next = currentDebugNode.Children
                        .FirstOrDefault(c => c.BranchType == ConnectionType.Normal);
                    break;
            }

            // 2. Вставляем следующий узел в последовательность выполнения
            if (next != null)
            {
                executionOrder.Insert(currentStepIndex + 1, next);
            }

            // 3. Переход к следующему шагу
            currentStepIndex++;
        }

        private void SetupBlockDragAndDrop()
        {
            var blocksList = BlocksListView;

            if (blocksList != null)
            {
                blocksList.DragItemsStarting += BlocksList_DragItemsStarting;
            }
        }
    }
}
