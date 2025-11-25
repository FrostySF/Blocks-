using Blocks_.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Blocks_.haru
{
    public static class EditWindow
    {
        public static UIElement Content { get; set; }

        public static async void Show(BlockItem block)
        {
            if (block.Type == BlockType.ArrayDeclaration)
            {
                await ShowArrayEditor(block);
                return;
            }

            var docsBox = new TextBlock
            {
                Text = block.Docs,
                TextWrapping = TextWrapping.WrapWholeWords,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var shotBox = new TextBlock
            {
                Text = block.Shot,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var codeBox = new TextBox { Text = block.Code, PlaceholderText = "Code", AcceptsReturn = true };

            var panel = new StackPanel();
            var debugText = new TextBlock
            {
                Text = $"DEBUG:\nDocs is null: {block.Docs == null}\nDocs length: {block.Docs?.Length ?? -1}\nShot is null: {block.Shot == null}\nShot length: {block.Shot?.Length ?? -1}",
                TextWrapping = TextWrapping.Wrap,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red)
            };
            panel.Children.Add(debugText);
            panel.Children.Add(docsBox);
            panel.Children.Add(shotBox);
            panel.Children.Add(codeBox);

            var scrollViewer = new ScrollViewer { Content = panel };

            var dialog = new ContentDialog
            {
                Title = $"Редактирование блока - {block.Name}",
                Content = scrollViewer,
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            bool isPrimaryResultSimulated = false;

            codeBox.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Enter)
                {
                    e.Handled = true;
                    isPrimaryResultSimulated = true;
                    dialog.Hide();
                }
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary || isPrimaryResultSimulated)
            {
                block.Code = codeBox.Text;
            }
        }

        private static async System.Threading.Tasks.Task ShowArrayEditor(BlockItem block)
        {
            var mainPanel = new StackPanel { Spacing = 10 };

            var titleText = new TextBlock
            {
                Text = "Редактор массивов",
                FontSize = 16,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            mainPanel.Children.Add(titleText);

            var typePanel = new StackPanel { Spacing = 5 };
            var typeLabel = new TextBlock { Text = "Тип массива:" };
            var typeCombo = new ComboBox { Width = 200 };
            typeCombo.Items.Add(new ComboBoxItem { Content = "Вектор (одномерный)", Tag = "vector" });
            typeCombo.Items.Add(new ComboBoxItem { Content = "Матрица (двумерный)", Tag = "matrix" });
            typeCombo.SelectedIndex = 0;
            typePanel.Children.Add(typeLabel);
            typePanel.Children.Add(typeCombo);
            mainPanel.Children.Add(typePanel);

            var namePanel = new StackPanel { Spacing = 5 };
            var nameLabel = new TextBlock { Text = "Имя массива:" };
            var nameBox = new TextBox { PlaceholderText = "Например: arr, matrix" };
            namePanel.Children.Add(nameLabel);
            namePanel.Children.Add(nameBox);
            mainPanel.Children.Add(namePanel);

            var vectorPanel = new StackPanel { Spacing = 5, Visibility = Visibility.Visible };
            var vectorSizeLabel = new TextBlock { Text = "Размер вектора:" };
            var vectorSizeBox = new TextBox { PlaceholderText = "Например: 5" };
            var vectorValuesLabel = new TextBlock { Text = "Начальные значения (через запятую):" };
            var vectorValuesBox = new TextBox { PlaceholderText = "Например: 1, 2, 3, 4, 5" };

            // Улучшенное поведение при фокусе - выделяем весь текст при клике
            vectorSizeBox.GotFocus += (s, e) =>
            {
                vectorSizeBox.SelectAll();
            };

            vectorValuesBox.GotFocus += (s, e) => vectorValuesBox.SelectAll();

            vectorPanel.Children.Add(vectorSizeLabel);
            vectorPanel.Children.Add(vectorSizeBox);
            vectorPanel.Children.Add(vectorValuesLabel);
            vectorPanel.Children.Add(vectorValuesBox);
            mainPanel.Children.Add(vectorPanel);

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

            // Улучшенное поведение для полей матрицы
            matrixRowsBox.GotFocus += (s, e) =>
            {
                matrixRowsBox.SelectAll();
            };

            matrixColsBox.GotFocus += (s, e) =>
            {
                matrixColsBox.SelectAll();
            };

            matrixValuesBox.GotFocus += (s, e) => matrixValuesBox.SelectAll();

            matrixPanel.Children.Add(matrixRowsLabel);
            matrixPanel.Children.Add(matrixRowsBox);
            matrixPanel.Children.Add(matrixColsLabel);
            matrixPanel.Children.Add(matrixColsBox);
            matrixPanel.Children.Add(matrixValuesLabel);
            matrixPanel.Children.Add(matrixValuesBox);
            mainPanel.Children.Add(matrixPanel);

            var helpText = new TextBlock
            {
                Text = "Примеры использования:\nВектор: arr[0], arr[i]\nМатрица: matrix[0][1], matrix[i][j]",
                FontSize = 11,
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap
            };
            mainPanel.Children.Add(helpText);

            typeCombo.SelectionChanged += (s, e) =>
            {
                if (typeCombo.SelectedItem is ComboBoxItem selected)
                {
                    bool isVector = (string)selected.Tag == "vector";
                    vectorPanel.Visibility = isVector ? Visibility.Visible : Visibility.Collapsed;
                    matrixPanel.Visibility = isVector ? Visibility.Collapsed : Visibility.Visible;
                }
            };

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
                        string size = vectorSizeBox.Text.Trim();
                        string values = vectorValuesBox.Text.Trim();

                        if (string.IsNullOrEmpty(size) || size == "0")
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
                        string rows = matrixRowsBox.Text.Trim();
                        string cols = matrixColsBox.Text.Trim();
                        string values = matrixValuesBox.Text.Trim();

                        if (string.IsNullOrEmpty(rows) || rows == "0" || string.IsNullOrEmpty(cols) || cols == "0")
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