using Blocks_.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Blocks_.haru
{
    public static class EditWindow
    {
        public static UIElement Content { get; set; }

        public static async void Show(Border blockControl)
        {
            if (blockControl.Tag is BlockItem block)
            {
                var dialog = new ContentDialog
                {
                    Title = $"Редактирование: {block.Name}",
                    Content = $"Редактирование: {block.Name}",
                    PrimaryButtonText = "Сохранить",
                    SecondaryButtonText = "Отмена",
                    XamlRoot = Content.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Логика сохранения изменений
                }
            }
        }

        public static async void Show(BlockItem block)
        {
            // Специальная обработка для блоков массивов
            if (block.Type == BlockType.ArrayDeclaration)
            {
                await ShowArrayEditor(block);
                return;
            }

            // Стандартное окно редактирования
            var nameBox = new TextBlock { Text = block.Description };
            var descBox = new TextBox { Text = block.Code, PlaceholderText = "Code", AcceptsReturn = true };

            var panel = new StackPanel();
            panel.Children.Add(nameBox);
            panel.Children.Add(descBox);

         

            var dialog = new ContentDialog
            {
                Title = $"Редактирование блока - {block.Name}",
                Content = panel,
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            bool isPrimaryResultSimulated = false;

            descBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    e.Handled = true;
                    isPrimaryResultSimulated = true; // 2. Устанавливаем флаг
                    dialog.Hide(); // 3. Вызываем Hide() без аргументов
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary || isPrimaryResultSimulated)
            {
                block.Code = descBox.Text;
            }
        }

        private static async System.Threading.Tasks.Task ShowArrayEditor(BlockItem block)
        {
            var mainPanel = new StackPanel { Spacing = 10 };

            // Заголовок
            var titleText = new TextBlock
            {
                Text = "Редактор массивов",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            mainPanel.Children.Add(titleText);

            // Выбор типа массива
            var typePanel = new StackPanel { Spacing = 5 };
            var typeLabel = new TextBlock { Text = "Тип массива:" };
            var typeCombo = new ComboBox { Width = 200 };
            typeCombo.Items.Add(new ComboBoxItem { Content = "Вектор (одномерный)", Tag = "vector" });
            typeCombo.Items.Add(new ComboBoxItem { Content = "Матрица (двумерный)", Tag = "matrix" });
            typeCombo.SelectedIndex = 0;
            typePanel.Children.Add(typeLabel);
            typePanel.Children.Add(typeCombo);
            mainPanel.Children.Add(typePanel);

            // Имя массива
            var namePanel = new StackPanel { Spacing = 5 };
            var nameLabel = new TextBlock { Text = "Имя массива:" };
            var nameBox = new TextBox { PlaceholderText = "Например: arr, matrix" };
            namePanel.Children.Add(nameLabel);
            namePanel.Children.Add(nameBox);
            mainPanel.Children.Add(namePanel);

            // Панель для параметров вектора
            var vectorPanel = new StackPanel { Spacing = 5, Visibility = Visibility.Visible };
            var vectorSizeLabel = new TextBlock { Text = "Размер вектора:" };
            var vectorSizeBox = new TextBox { PlaceholderText = "Например: 5" };
            var vectorValuesLabel = new TextBlock { Text = "Начальные значения (через запятую):" };
            var vectorValuesBox = new TextBox { PlaceholderText = "Например: 1, 2, 3, 4, 5", AcceptsReturn = true };
            vectorPanel.Children.Add(vectorSizeLabel);
            vectorPanel.Children.Add(vectorSizeBox);
            vectorPanel.Children.Add(vectorValuesLabel);
            vectorPanel.Children.Add(vectorValuesBox);
            mainPanel.Children.Add(vectorPanel);

            // Панель для параметров матрицы
            var matrixPanel = new StackPanel { Spacing = 5, Visibility = Visibility.Collapsed };
            var matrixRowsLabel = new TextBlock { Text = "Количество строк:" };
            var matrixRowsBox = new TextBox { PlaceholderText = "Например: 3" };
            var matrixColsLabel = new TextBlock { Text = "Количество столбцов:" };
            var matrixColsBox = new TextBox { PlaceholderText = "Например: 3" };
            var matrixValuesLabel = new TextBlock { Text = "Начальные значения (строки через ;):" };
            var matrixValuesBox = new TextBox
            {
                PlaceholderText = "Например: 1,2,3;4,5,6;7,8,9",
                AcceptsReturn = true,
                Height = 80
            };
            matrixPanel.Children.Add(matrixRowsLabel);
            matrixPanel.Children.Add(matrixRowsBox);
            matrixPanel.Children.Add(matrixColsLabel);
            matrixPanel.Children.Add(matrixColsBox);
            matrixPanel.Children.Add(matrixValuesLabel);
            matrixPanel.Children.Add(matrixValuesBox);
            mainPanel.Children.Add(matrixPanel);

            // Справка
            var helpText = new TextBlock
            {
                Text = "Примеры использования:\nВектор: arr[0], arr[i]\nМатрица: matrix[0][1], matrix[i][j]",
                FontSize = 11,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };
            mainPanel.Children.Add(helpText);

            // Обработчик переключения типа массива
            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem is ComboBoxItem selected)
                {
                    bool isVector = (string)selected.Tag == "vector";
                    vectorPanel.Visibility = isVector ? Visibility.Visible : Visibility.Collapsed;
                    matrixPanel.Visibility = isVector ? Visibility.Collapsed : Visibility.Visible;
                }
            };

            // Парсинг существующего кода (если есть)
            if (!string.IsNullOrWhiteSpace(block.Code))
            {
                ParseArrayCode(block.Code, nameBox, typeCombo, vectorSizeBox, vectorValuesBox,
                              matrixRowsBox, matrixColsBox, matrixValuesBox, vectorPanel, matrixPanel);
            }

            var dialog = new ContentDialog
            {
                Title = "Объявление массива",
                Content = new ScrollViewer { Content = mainPanel, MaxHeight = 500 },
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Формируем код массива
                string arrayName = nameBox.Text.Trim();
                if (string.IsNullOrEmpty(arrayName))
                {
                    await ShowError("Имя массива не может быть пустым!");
                    return;
                }

                if (typeCombo.SelectedItem is ComboBoxItem selectedType)
                {
                    bool isVector = (string)selectedType.Tag == "vector";

                    if (isVector)
                    {
                        // Формат: arrName[size] = {val1, val2, ...}
                        string size = vectorSizeBox.Text.Trim();
                        string values = vectorValuesBox.Text.Trim();

                        if (string.IsNullOrEmpty(size))
                        {
                            block.Code = $"{arrayName}[]";
                        }
                        else if (string.IsNullOrEmpty(values))
                        {
                            block.Code = $"{arrayName}[{size}]";
                        }
                        else
                        {
                            block.Code = $"{arrayName}[{size}] = {{{values}}}";
                        }
                    }
                    else
                    {
                        // Формат: matrixName[rows][cols] = {{val1,val2},{val3,val4},...}
                        string rows = matrixRowsBox.Text.Trim();
                        string cols = matrixColsBox.Text.Trim();
                        string values = matrixValuesBox.Text.Trim();

                        if (string.IsNullOrEmpty(rows) || string.IsNullOrEmpty(cols))
                        {
                            block.Code = $"{arrayName}[][]";
                        }
                        else if (string.IsNullOrEmpty(values))
                        {
                            block.Code = $"{arrayName}[{rows}][{cols}]";
                        }
                        else
                        {
                            // Преобразуем формат "1,2,3;4,5,6" в "{{1,2,3},{4,5,6}}"
                            var rowsArray = values.Split(';');
                            var formattedValues = "{" + string.Join("},{", rowsArray) + "}";
                            block.Code = $"{arrayName}[{rows}][{cols}] = {{{formattedValues}}}";
                        }
                    }
                }

                block.Name = $"Массив: {arrayName}";
            }
        }

        private static void ParseArrayCode(string code, TextBox nameBox, ComboBox typeCombo,
            TextBox vectorSizeBox, TextBox vectorValuesBox,
            TextBox matrixRowsBox, TextBox matrixColsBox, TextBox matrixValuesBox,
            StackPanel vectorPanel, StackPanel matrixPanel)
        {
            try
            {
                // Простой парсер для восстановления значений из кода
                if (code.Contains("[][]"))
                {
                    // Матрица
                    typeCombo.SelectedIndex = 1;
                    vectorPanel.Visibility = Visibility.Collapsed;
                    matrixPanel.Visibility = Visibility.Visible;

                    var parts = code.Split('[');
                    if (parts.Length > 0)
                        nameBox.Text = parts[0].Trim();
                }
                else if (code.Contains("["))
                {
                    // Вектор
                    typeCombo.SelectedIndex = 0;
                    vectorPanel.Visibility = Visibility.Visible;
                    matrixPanel.Visibility = Visibility.Collapsed;

                    var parts = code.Split('[');
                    if (parts.Length > 0)
                        nameBox.Text = parts[0].Trim();
                }
            }
            catch
            {
                // Если парсинг не удался, оставляем значения по умолчанию
            }
        }

        private static async System.Threading.Tasks.Task ShowError(string message)
        {
            var errorDialog = new ContentDialog
            {
                Title = "Ошибка",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await errorDialog.ShowAsync();
        }
    }
}