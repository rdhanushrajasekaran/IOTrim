using System;
using IOTrim.Service;
using System.Windows;
using System.Windows.Controls;

namespace IOTrim.Views
{
    public partial class OEEPage : Page
    {
        private readonly OeeDashboard oeeDashboard = new OeeDashboard();
        private readonly OEEHourlyData hourlyData = new OEEHourlyData();

        public OEEPage()
        {
            try
            {
                InitializeComponent();
                OEEFrm.Navigate(oeeDashboard);
                LogService.AddLog("OEE page initialized. Inner frame loaded: OEE dashboard.");
            }
            catch (Exception ex)
            {
                LogService.AddException("OEE page initialization failed", ex);
                throw;
            }
        }

        private void OeeNavBtnClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedBtn)
                return;

            string tag = clickedBtn.Tag?.ToString() ?? string.Empty;

            try
            {
                switch (tag)
                {
                    case "Dashboard":
                        OEEFrm.Navigate(oeeDashboard);
                        LogService.AddLog("OEE inner frame navigated to OEE dashboard.");
                        break;

                    case "HourlyData":
                        OEEFrm.Navigate(hourlyData);
                        LogService.AddLog("OEE inner frame navigated to Hourly Data.");
                        break;
                    default:
                        OEEFrm.Navigate(oeeDashboard);
                        LogService.AddLog("Unknown OEE inner navigation target. Loaded OEE dashboard.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"OEE inner navigation failed. Target:{tag}", ex);
                MessageBox.Show(ex.Message, "OEE Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
