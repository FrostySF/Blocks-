using Blocks_.Core.Models;
using Blocks_.haru;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Показывает диалог редактирования цикла While
        /// </summary>
        public async Task ShowWhileEditDialog(BlockItem block)
        {
            var panel = new StackPanel { Spacing = 15 };

            // Название цикла
            var nameLabel = new TextBlock { Text = "Название цикла:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var nameBox = new TextBox
            {
                Text = block.Name,
                PlaceholderText = "WHILE1"
            };

            // Режим ввода
            var modeLabel = new TextBlock { Text = "Режим ввода:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var modeCombo = new ComboBox { SelectedIndex = 0 };
            modeCombo.Items.Add(new ComboBoxItem { Content = "Текстовый ввод" });
            modeCombo.Items.Add(new ComboBoxItem { Content = "Шаблон" });

            // Контейнер для динамического содержимого
            var contentPanel = new StackPanel { Spacing = 10 };

            // Текстовый режим (по умолчанию)
            var textLabel = new TextBlock { Text = "Условие цикла:" };
            var codeBox = new TextBox
            {
                Text = block.Code ?? "i < 10",
                PlaceholderText = "Например: i < 10",
                AcceptsReturn = false
            };

            contentPanel.Children.Add(textLabel);
            contentPanel.Children.Add(codeBox);

            // Обработчик смены режима
            modeCombo.SelectionChanged += (s, e) =>
            {
                contentPanel.Children.Clear();

                if (modeCombo.SelectedIndex == 0)
                {
                    // Текстовый режим
                    contentPanel.Children.Add(textLabel);
                    contentPanel.Children.Add(codeBox);
                }
                else
                {
                    // Режим шаблона (для while - просто дублирует текстовый)
                    var templateLabel = new TextBlock { Text = "Условие продолжения цикла:" };
                    contentPanel.Children.Add(templateLabel);
                    contentPanel.Children.Add(codeBox);
                }
            };

            panel.Children.Add(nameLabel);
            panel.Children.Add(nameBox);
            panel.Children.Add(modeLabel);
            panel.Children.Add(modeCombo);
            panel.Children.Add(contentPanel);

            var dialog = new ContentDialog
            {
                Title = "Редактирование цикла WHILE",
                Content = new ScrollViewer { Content = panel, MaxHeight = 500 },
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                block.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? block.Name : nameBox.Text;
                block.Code = codeBox.Text;
                UpdateBlockVisual(block);
            }
        }

        /// <summary>
        /// Показывает диалог редактирования цикла Do-While
        /// </summary>
        public async Task ShowDoWhileEditDialog(BlockItem block)
        {
            var panel = new StackPanel { Spacing = 15 };

            // Название цикла
            var nameLabel = new TextBlock { Text = "Название цикла:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var nameBox = new TextBox
            {
                Text = block.Name,
                PlaceholderText = "DO-WHILE1"
            };

            // Режим ввода
            var modeLabel = new TextBlock { Text = "Режим ввода:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var modeCombo = new ComboBox { SelectedIndex = 0 };
            modeCombo.Items.Add(new ComboBoxItem { Content = "Текстовый ввод" });
            modeCombo.Items.Add(new ComboBoxItem { Content = "Шаблон" });

            // Контейнер для динамического содержимого
            var contentPanel = new StackPanel { Spacing = 10 };

            // Текстовый режим (по умолчанию)
            var textLabel = new TextBlock { Text = "Условие повтора (WHILE):" };
            var codeBox = new TextBox
            {
                Text = block.Code ?? "i < 10",
                PlaceholderText = "Например: i < 10",
                AcceptsReturn = false
            };

            contentPanel.Children.Add(textLabel);
            contentPanel.Children.Add(codeBox);

            // Обработчик смены режима
            modeCombo.SelectionChanged += (s, e) =>
            {
                contentPanel.Children.Clear();

                if (modeCombo.SelectedIndex == 0)
                {
                    // Текстовый режим
                    contentPanel.Children.Add(textLabel);
                    contentPanel.Children.Add(codeBox);
                }
                else
                {
                    // Режим шаблона
                    var templateLabel = new TextBlock { Text = "Условие повтора (проверяется ПОСЛЕ выполнения тела):" };
                    var hintText = new TextBlock
                    {
                        Text = "Цикл DO-WHILE выполняется минимум 1 раз",
                        FontSize = 12,
                        Opacity = 0.7
                    };
                    contentPanel.Children.Add(templateLabel);
                    contentPanel.Children.Add(codeBox);
                    contentPanel.Children.Add(hintText);
                }
            };

            panel.Children.Add(nameLabel);
            panel.Children.Add(nameBox);
            panel.Children.Add(modeLabel);
            panel.Children.Add(modeCombo);
            panel.Children.Add(contentPanel);

            var dialog = new ContentDialog
            {
                Title = "Редактирование цикла DO-WHILE",
                Content = new ScrollViewer { Content = panel, MaxHeight = 500 },
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                block.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? block.Name : nameBox.Text;
                block.Code = codeBox.Text;
                UpdateBlockVisual(block);
            }
        }

        /// <summary>
        /// Показывает диалог редактирования цикла For
        /// </summary>
        public async Task ShowForEditDialog(BlockItem block)
        {
            var panel = new StackPanel { Spacing = 15 };

            // Название цикла
            var nameLabel = new TextBlock { Text = "Название цикла:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var nameBox = new TextBox
            {
                Text = block.Name,
                PlaceholderText = "FOR1"
            };

            // Режим ввода
            var modeLabel = new TextBlock { Text = "Режим ввода:", FontWeight = Microsoft.UI.Text.FontWeights.Bold };
            var modeCombo = new ComboBox { SelectedIndex = 0 };
            modeCombo.Items.Add(new ComboBoxItem { Content = "Текстовый ввод" });
            modeCombo.Items.Add(new ComboBoxItem { Content = "Шаблон" });

            var contentPanel = new StackPanel { Spacing = 10 };

            var textLabel = new TextBlock { Text = "Конструкция цикла (init; condition; step):" };
            var codeBox = new TextBox
            {
                Text = block.Code ?? "i = 0; i < 10; i = i + 1",
                PlaceholderText = "Например: i = 0; i < 10; i = i + 1",
                AcceptsReturn = false
            };

            contentPanel.Children.Add(textLabel);
            contentPanel.Children.Add(codeBox);

  
            TextBox initBox = null, condBox = null, stepBox = null;

            // Обработчик смены режима
            modeCombo.SelectionChanged += (s, e) =>
            {
                contentPanel.Children.Clear();

                if (modeCombo.SelectedIndex == 0)
                {
                    // Текстовый режим
                    contentPanel.Children.Add(textLabel);
                    contentPanel.Children.Add(codeBox);
                }
                else
                {
                    // Режим шаблона
                    var parts = (block.Code ?? "i = 0; i < 10; i = i + 1").Split(';');

                    var initLabel = new TextBlock { Text = "1. Инициализация (переменная = значение):" };
                    initBox = new TextBox
                    {
                        Text = parts.Length > 0 ? parts[0].Trim() : "i = 0",
                        PlaceholderText = "i = 0"
                    };

                    var condLabel = new TextBlock { Text = "2. Условие продолжения:" };
                    condBox = new TextBox
                    {
                        Text = parts.Length > 1 ? parts[1].Trim() : "i < 10",
                        PlaceholderText = "i < 10"
                    };

                    var stepLabel = new TextBlock { Text = "3. Шаг (инкремент/декремент):" };
                    stepBox = new TextBox
                    {
                        Text = parts.Length > 2 ? parts[2].Trim() : "i = i + 1",
                        PlaceholderText = "i = i + 1"
                    };

                    contentPanel.Children.Add(initLabel);
                    contentPanel.Children.Add(initBox);
                    contentPanel.Children.Add(condLabel);
                    contentPanel.Children.Add(condBox);
                    contentPanel.Children.Add(stepLabel);
                    contentPanel.Children.Add(stepBox);

                    // Синхронизация с текстовым режимом
                    void UpdateCodeBox()
                    {
                        codeBox.Text = $"{initBox.Text}; {condBox.Text}; {stepBox.Text}";
                    }

                    initBox.TextChanged += (_, _) => UpdateCodeBox();
                    condBox.TextChanged += (_, _) => UpdateCodeBox();
                    stepBox.TextChanged += (_, _) => UpdateCodeBox();
                }
            };

            panel.Children.Add(nameLabel);
            panel.Children.Add(nameBox);
            panel.Children.Add(modeLabel);
            panel.Children.Add(modeCombo);
            panel.Children.Add(contentPanel);

            var dialog = new ContentDialog
            {
                Title = "Редактирование цикла FOR",
                Content = new ScrollViewer { Content = panel, MaxHeight = 500 },
                PrimaryButtonText = "Сохранить",
                CloseButtonText = "Отмена",
                XamlRoot = Content.XamlRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                block.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? block.Name : nameBox.Text;

                // Если был режим шаблона, собираем код из полей
                if (modeCombo.SelectedIndex == 1 && initBox != null && condBox != null && stepBox != null)
                {
                    block.Code = $"{initBox.Text}; {condBox.Text}; {stepBox.Text}";
                }
                else
                {
                    block.Code = codeBox.Text;
                }

                UpdateBlockVisual(block);
            }
        }
    }
}