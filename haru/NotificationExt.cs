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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;

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
            // Проверка, что элементы UI инициализированы
            if (NotificationContainer == null || NotificationTextBlock == null)
            {
                return;
            }

            // 1. Устанавливаем сообщение
            NotificationTextBlock.Text = message;

            // 2. Анимация появления (Fade In)
            var storyboard = new Storyboard();

            var fadeInAnimation = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.3))
            };
            Storyboard.SetTarget(fadeInAnimation, NotificationContainer);
            Storyboard.SetTargetProperty(fadeInAnimation, "Opacity");
            storyboard.Children.Add(fadeInAnimation);

            // Сначала делаем элемент видимым, затем запускаем анимацию
            NotificationContainer.Visibility = Visibility.Visible;
            storyboard.Begin();

            // 3. Настройка таймера для автоматического скрытия
            if (notificationTimer != null)
            {
                notificationTimer.Stop(); // Останавливаем предыдущий таймер
            }

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

            // Анимация исчезновения (Fade Out)
            var fadeOutAnimation = new DoubleAnimation
            {
                To = 0.0,
                Duration = new Duration(TimeSpan.FromSeconds(0.5))
            };
            Storyboard.SetTarget(fadeOutAnimation, NotificationContainer);
            Storyboard.SetTargetProperty(fadeOutAnimation, "Opacity");
            storyboard.Children.Add(fadeOutAnimation);

            // После завершения анимации делаем элемент Collapsed (не занимает место в макете)
            storyboard.Completed += (s, e) =>
            {
                NotificationContainer.Visibility = Visibility.Collapsed;
            };

            storyboard.Begin();
        }
    }
}
