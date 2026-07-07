using IOTrim.Service;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace IOTrim
{
    public partial class App : Application
    {

        public const string AppName = "Global\\IOTrim";
        private Mutex? _mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            bool isNewInstance;

            _mutex = new Mutex(true, AppName, out isNewInstance);
            
             if(!isNewInstance)
             {
                MessageBox.Show("Another instance of the application is already running.", "Instance Running", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
             }
            

            base.OnStartup(e);

            LogService.Start();
            LogService.AddLog("Application startup started.");
            ThemeService.ApplyTheme(AppTheme.Light);

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogService.AddException("UI unhandled exception", e.Exception);

            MessageBox.Show(
                e.Exception.Message,
                "Application Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
                LogService.AddException("Application domain unhandled exception", ex);
            else
                LogService.AddLog("Application domain unhandled exception: " + e.ExceptionObject);
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogService.AddException("Unobserved background task exception", e.Exception);
            e.SetObserved();
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            LogService.AddLog("Application process exiting.");
            LogService.Stop();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            LogService.AddLog($"Application exited. ExitCode:{e.ApplicationExitCode}");
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
            LogService.Stop();
            base.OnExit(e);
        }
    }
}
