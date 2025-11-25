using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Blocks_
{
    public sealed partial class MainWindow : Window
    {
        private DispatcherTimer notificationTimer;

        /// <summary>
        /// Показывает всплывающее уведомление с заданным сообщением.
        /// </summary>
        /// <param name="message">Текст уведомления.</param>
        /// <param name="durationInSeconds">Время отображения уведомления в секундах.</param>
        public void ShowNotification(string message, int durationInSeconds = 3)
        {
            if (!SettingsWindow.AppSettings.ShowNotifications)
                return;
            if (NotificationContainer == null || NotificationTextBlock == null)
                return;
            

            NotificationTextBlock.Text = message;
            var storyboard = new Storyboard();

            var fadeInAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            Storyboard.SetTarget(fadeInAnimation, NotificationContainer);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            storyboard.Children.Add(fadeInAnimation);

            NotificationContainer.Visibility = Visibility.Visible;
            storyboard.Begin();

            if (notificationTimer != null)
                notificationTimer.Stop();
            notificationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationInSeconds)
            };
            notificationTimer.Tick += (sender, e) =>
            {
                notificationTimer.Stop();
                HideNotification();
            };
            notificationTimer.Start();
        }

        /// <summary>
        /// Плавно скрывает уведомление.
        /// </summary>
        private void HideNotification()
        {
            var storyboard = new Storyboard();
            var fadeOutAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            Storyboard.SetTarget(fadeOutAnimation, NotificationContainer);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
            storyboard.Children.Add(fadeOutAnimation);
            storyboard.Completed += (s, e) =>
            {
                NotificationContainer.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin();
        }
    }
}
