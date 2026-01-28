using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Storage;

namespace Blocks_
{
    public sealed partial class SettingsWindow : Window
    {
        private ApplicationDataContainer localSettings;
        public event EventHandler SettingsChanged;

        public SettingsWindow()
        {
            this.InitializeComponent();

            localSettings = ApplicationData.Current.LocalSettings;

            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            this.AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            var windowSize = new Windows.Graphics.SizeInt32(600, 700);
            this.AppWindow.Resize(windowSize);

            LoadSettings();
            ApplyCurrentTheme();

            PanningSensitivitySlider.ValueChanged += (s, e) =>
            {
                PanningSensitivityValue.Text = e.NewValue.ToString("F1");
            };
        }

        private void LoadSettings()
        {
            GridStepNumberBox.Value = GetSettingValue("GridStep", 100.0);
            HighlightRadiusNumberBox.Value = GetSettingValue("HighlightRadius", 1.0);
            ShowGridToggle.IsOn = GetSettingValue("ShowGrid", true);
            string theme = GetSettingValue("Theme", "Dark");
            ThemeComboBox.SelectedIndex = theme switch
            {
                "Dark" => 0,
                "Light" => 1,
                "Default" => 2,
                _ => 0
            };

            string accentColor = GetSettingValue("AccentColor", "Blue");
            AccentColorComboBox.SelectedIndex = accentColor switch
            {
                "Blue" => 0,
                "Green" => 1,
                "Purple" => 2,
                "Red" => 3,
                "Orange" => 4,
                _ => 0
            };

            PanningSensitivitySlider.Value = GetSettingValue("PanningSensitivity", 3.5);
            PanningSensitivityValue.Text = PanningSensitivitySlider.Value.ToString("F1");
            ZoomStepNumberBox.Value = GetSettingValue("ZoomStep", 10.0);
            // AnimationsToggle.IsOn = GetSettingValue("Animations", true);

            MaxUndoStepsNumberBox.Value = GetSettingValue("MaxUndoSteps", 50.0);
            AutoSaveToggle.IsOn = GetSettingValue("AutoSave", false);
            AutoSaveIntervalNumberBox.Value = GetSettingValue("AutoSaveInterval", 5.0);
            SnapToGridToggle.IsOn = GetSettingValue("SnapToGrid", true);
            ShowNotificationsToggle.IsOn = GetSettingValue("ShowNotifications", true);

            MinSegmentLengthNumberBox.Value = GetSettingValue("MinSegmentLength", 10.0);
            ObstacleClearanceNumberBox.Value = GetSettingValue("ObstacleClearance", 15.0);
            //SmartRoutingToggle.IsOn = GetSettingValue("SmartRouting", true);
        }

        private void ApplyCurrentTheme()
        {
            string theme = GetSettingValue("Theme", "Dark");
            ApplyTheme(theme);
        }

        private string GetSettingValue(string key, string defaultValue)
        {
            if (localSettings.Values.ContainsKey(key))
                return localSettings.Values[key]?.ToString() ?? defaultValue;
            return defaultValue;
        }

        private double GetSettingValue(string key, double defaultValue)
        {
            if (localSettings.Values.ContainsKey(key))
                return Convert.ToDouble(localSettings.Values[key]);
            return defaultValue;
        }

        private bool GetSettingValue(string key, bool defaultValue)
        {
            if (localSettings.Values.ContainsKey(key))
                return Convert.ToBoolean(localSettings.Values[key]);
            return defaultValue;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            localSettings.Values["GridStep"] = GridStepNumberBox.Value;
            localSettings.Values["HighlightRadius"] = (int)HighlightRadiusNumberBox.Value;
            localSettings.Values["ShowGrid"] = ShowGridToggle.IsOn;

            if (ThemeComboBox.SelectedItem is ComboBoxItem themeItem)
                localSettings.Values["Theme"] = themeItem.Tag.ToString();
            if (AccentColorComboBox.SelectedItem is ComboBoxItem accentItem)
                localSettings.Values["AccentColor"] = accentItem.Tag.ToString();

            localSettings.Values["PanningSensitivity"] = PanningSensitivitySlider.Value;
            localSettings.Values["ZoomStep"] = ZoomStepNumberBox.Value;
            //localSettings.Values["Animations"] = AnimationsToggle.IsOn;

            localSettings.Values["MaxUndoSteps"] = (int)MaxUndoStepsNumberBox.Value;
            localSettings.Values["AutoSave"] = AutoSaveToggle.IsOn;
            localSettings.Values["AutoSaveInterval"] = (int)AutoSaveIntervalNumberBox.Value;
            localSettings.Values["SnapToGrid"] = SnapToGridToggle.IsOn;
            localSettings.Values["ShowNotifications"] = ShowNotificationsToggle.IsOn;

            localSettings.Values["MinSegmentLength"] = MinSegmentLengthNumberBox.Value;
            localSettings.Values["ObstacleClearance"] = ObstacleClearanceNumberBox.Value;
            //localSettings.Values["SmartRouting"] = SmartRoutingToggle.IsOn;

            SettingsChanged?.Invoke(this, EventArgs.Empty);
            var timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += (s, args) =>
            {
                timer.Stop();
                this.Close();
            };
            timer.Start();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            GridStepNumberBox.Value = 100;
            HighlightRadiusNumberBox.Value = 1;
            ShowGridToggle.IsOn = true;

            ThemeComboBox.SelectedIndex = 0;
            AccentColorComboBox.SelectedIndex = 0;

            PanningSensitivitySlider.Value = 3.5;
            ZoomStepNumberBox.Value = 10;
            // AnimationsToggle.IsOn = true;

            MaxUndoStepsNumberBox.Value = 50;
            AutoSaveToggle.IsOn = false;
            AutoSaveIntervalNumberBox.Value = 5;
            SnapToGridToggle.IsOn = true;
            ShowNotificationsToggle.IsOn = true;

            MinSegmentLengthNumberBox.Value = 10;
            ObstacleClearanceNumberBox.Value = 15;
            //SmartRoutingToggle.IsOn = true;
        }

        private void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThemeComboBox.SelectedItem is ComboBoxItem item)
            {
                string theme = item.Tag.ToString();
                ApplyTheme(theme);
            }
        }

        private void ApplyTheme(string theme)
        {
            var elementTheme = theme switch
            {
                "Dark" => ElementTheme.Dark,
                "Light" => ElementTheme.Light,
                _ => ElementTheme.Default
            };

            if (this.Content is FrameworkElement rootElement)
            {
                rootElement.RequestedTheme = elementTheme;
            }

            // Обновляем цвета заголовка окна в зависимости от темы
            UpdateTitleBarColors(elementTheme);
        }

        private void UpdateTitleBarColors(ElementTheme theme)
        {
            if (theme == ElementTheme.Light)
            {
                // Светлая тема - темные кнопки
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                this.AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.Black;
                this.AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.Black;
            }
            else
            {
                // Темная тема - светлые кнопки
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                this.AppWindow.TitleBar.ButtonHoverForegroundColor = Colors.White;
                this.AppWindow.TitleBar.ButtonPressedForegroundColor = Colors.White;
            }
        }

        public static class AppSettings
        {
            private static ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;

            public static int GridStep => GetInt("GridStep", 100);
            public static int HighlightRadius => GetInt("HighlightRadius", 1);
            public static bool ShowGrid => GetBool("ShowGrid", true);

            public static string Theme => GetString("Theme", "Dark");
            public static string AccentColor => GetString("AccentColor", "Blue");

            public static double PanningSensitivity => GetDouble("PanningSensitivity", 3.5);
            public static double ZoomStep => GetDouble("ZoomStep", 10.0);
            public static bool Animations => GetBool("Animations", true);

            public static int MaxUndoSteps => GetInt("MaxUndoSteps", 50);
            public static bool AutoSave => GetBool("AutoSave", false);
            public static int AutoSaveInterval => GetInt("AutoSaveInterval", 5);
            public static bool SnapToGrid => GetBool("SnapToGrid", true);
            public static bool ShowNotifications => GetBool("ShowNotifications", true);

            public static double MinSegmentLength => GetDouble("MinSegmentLength", 10.0);
            public static double ObstacleClearance => GetDouble("ObstacleClearance", 15.0);
            public static bool SmartRouting => GetBool("SmartRouting", true);

            private static string GetString(string key, string defaultValue)
            {
                if (settings.Values.ContainsKey(key))
                    return settings.Values[key]?.ToString() ?? defaultValue;
                return defaultValue;
            }

            private static int GetInt(string key, int defaultValue)
            {
                if (settings.Values.ContainsKey(key))
                    return Convert.ToInt32(settings.Values[key]);
                return defaultValue;
            }

            private static double GetDouble(string key, double defaultValue)
            {
                if (settings.Values.ContainsKey(key))
                    return Convert.ToDouble(settings.Values[key]);
                return defaultValue;
            }

            private static bool GetBool(string key, bool defaultValue)
            {
                if (settings.Values.ContainsKey(key))
                    return Convert.ToBoolean(settings.Values[key]);
                return defaultValue;
            }
        }
    }
}