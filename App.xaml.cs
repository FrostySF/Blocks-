using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Blocks_
{
    public partial class App : Application
    {
        private MainWindow? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            if (_window == null)
            {
                _window = new MainWindow();
                _window.Activate();
            }
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedArgs.Kind == ExtendedActivationKind.File)
                if (activatedArgs.Data is IFileActivatedEventArgs fileArgs)
                    if (fileArgs.Files.FirstOrDefault() is StorageFile file)
                        await _window.LoadFlowchartFromFile(file);

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                File.WriteAllText("crash.txt", e.ExceptionObject.ToString());
            };
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                File.WriteAllText("crash_task.txt", e.Exception.ToString());
            };
        }
    }
}