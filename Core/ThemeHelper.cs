using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using Windows.UI;

namespace Blocks_.Core
{
    public static class ThemeHelper
    {
        /// <summary>
        /// Применяет тему к элементу
        /// </summary>
        public static void ApplyTheme(FrameworkElement element, string theme)
        {
            var elementTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };

            element.RequestedTheme = elementTheme;
        }

        /// <summary>
        /// Получает цвет по названию
        /// </summary>
        public static Color GetAccentColor(string colorName)
        {
            return colorName switch
            {
                "Blue" => Color.FromArgb(255, 0, 120, 215),
                "Green" => Color.FromArgb(255, 16, 124, 16),
                "Purple" => Color.FromArgb(255, 136, 23, 152),
                "Red" => Color.FromArgb(255, 232, 17, 35),
                "Orange" => Color.FromArgb(255, 247, 99, 12),
                _ => Color.FromArgb(255, 0, 120, 215)
            };
        }

        /// <summary>
        /// Применяет цвет акцента к приложению
        /// </summary>
        public static void ApplyAccentColor(string colorName)
        {
            Color color = GetAccentColor(colorName);
            if (Application.Current.Resources.ContainsKey("SystemAccentColor"))
                Application.Current.Resources["SystemAccentColor"] = color;
            if (Application.Current.Resources.ContainsKey("SystemAccentColorLight1"))
                Application.Current.Resources["SystemAccentColorLight1"] = LightenColor(color, 0.2f);
            if (Application.Current.Resources.ContainsKey("SystemAccentColorLight2"))
                Application.Current.Resources["SystemAccentColorLight2"] = LightenColor(color, 0.4f);
            if (Application.Current.Resources.ContainsKey("SystemAccentColorDark1"))
                Application.Current.Resources["SystemAccentColorDark1"] = DarkenColor(color, 0.2f);
        }

        /// <summary>
        /// Осветляет цвет
        /// </summary>
        private static Color LightenColor(Color color, float factor)
        {
            byte r = (byte)Math.Min(255, color.R + (255 - color.R) * factor);
            byte g = (byte)Math.Min(255, color.G + (255 - color.G) * factor);
            byte b = (byte)Math.Min(255, color.B + (255 - color.B) * factor);

            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// Затемняет цвет
        /// </summary>
        private static Color DarkenColor(Color color, float factor)
        {
            byte r = (byte)(color.R * (1 - factor));
            byte g = (byte)(color.G * (1 - factor));
            byte b = (byte)(color.B * (1 - factor));

            return Color.FromArgb(color.A, r, g, b);
        }

        /// <summary>
        /// Проверяет, используется ли тёмная тема
        /// </summary>
        public static bool IsDarkTheme(FrameworkElement element) => element.ActualTheme == ElementTheme.Dark;

        /// <summary>
        /// Получает контрастный цвет текста для фона
        /// </summary>
        public static Color GetContrastTextColor(Color backgroundColor)
        {
            double brightness = (backgroundColor.R * 299 + backgroundColor.G * 587 + backgroundColor.B * 114) / 1000.0;

            return brightness > 128 ? Colors.Black : Colors.White;
        }
    }
}