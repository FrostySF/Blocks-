using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;

namespace Blocks_
{
    public sealed partial class HomePage : UserControl
    {
        public event EventHandler<RoutedEventArgs> NewFlowchartRequested;
        public event EventHandler<RoutedEventArgs> OpenFlowchartRequested;
        public event EventHandler<RecentFileEventArgs> RecentFileSelected;

        public ObservableCollection<RecentFileItem> RecentFiles { get; } = new ObservableCollection<RecentFileItem>();

        public HomePage()
        {
            this.InitializeComponent();
            LoadRecentFiles();
            RecentFilesListView.ItemsSource = RecentFiles;
        }

        private void LoadRecentFiles()
        {
            // TODO: Загрузить недавние файлы из настроек
            // Пример данных:
            RecentFiles.Add(new RecentFileItem
            {
                FileName = "Алгоритм сортировки.xml",
                FilePath = "C:\\Users\\User\\Documents\\sorting.xml",
                LastOpened = "Сегодня в 14:30"
            });

            RecentFiles.Add(new RecentFileItem
            {
                FileName = "Расчет факториала.xml",
                FilePath = "C:\\Users\\User\\Documents\\factorial.xml",
                LastOpened = "Вчера"
            });
        }

        private void NewFlowchartButton_Click(object sender, RoutedEventArgs e)
        {
            NewFlowchartRequested?.Invoke(this, e);
        }

        private void OpenFlowchartButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFlowchartRequested?.Invoke(this, e);
        }

        private void RecentFilesListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is RecentFileItem recentFile)
            {
                RecentFileSelected?.Invoke(this, new RecentFileEventArgs { RecentFile = recentFile });
            }
        }
    }

    public class RecentFileItem
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string LastOpened { get; set; }
    }

    public class RecentFileEventArgs : EventArgs
    {
        public RecentFileItem RecentFile { get; set; }
    }
}