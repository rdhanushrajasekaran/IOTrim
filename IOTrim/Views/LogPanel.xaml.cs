using IOTrim.Service;
using System;
using System.Windows;
using System.Windows.Controls;

namespace IOTrim.Views
{
    public partial class LogPanel : UserControl
    {
        public event EventHandler? CloseRequested;

        public LogPanel()
        {
            InitializeComponent();

            LogService.LogReceived += LogService_LogReceived;
        }

        private void LogService_LogReceived(string log)
        {
            txtLogs.AppendText(log + Environment.NewLine);
            txtLogs.ScrollToEnd();
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            txtLogs.Clear();
        }

        private void BtnCloseLog_Click(object sender, RoutedEventArgs e)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}