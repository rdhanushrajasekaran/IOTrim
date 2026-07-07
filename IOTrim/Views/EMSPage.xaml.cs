using System;
using IOTrim.Service;
using IOTrim.Views.EMS;
using System.Windows;
using System.Windows.Controls;

namespace IOTrim.Views
{
    public partial class EMSPage : Page
    {
        private readonly EMSDashboard eMSDashboard = new EMSDashboard();
        private readonly EMSHourlyData hourlyData = new EMSHourlyData();

        public EMSPage()
        {
            try
            {
                InitializeComponent();
                EMSFrm.Navigate(eMSDashboard);
                LogService.AddLog("EMS page initialized. Inner frame loaded: EMS dashboard.");
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS page initialization failed", ex);
                throw;
            }
        }

        private void EMSNavBtnClicked(object sender, RoutedEventArgs e)
        {
            if (sender is not Button clickedBtn)
                return;

            string tag = clickedBtn.Tag?.ToString() ?? string.Empty;

            try
            {
                switch (tag)
                {
                    case "Dashboard":
                        EMSFrm.Navigate(eMSDashboard);
                        LogService.AddLog("EMS inner frame navigated to EMS dashboard.");
                        break;

                    case "HourlyData":
                        EMSFrm.Navigate(hourlyData);
                        LogService.AddLog("EMS inner frame navigated to Hourly Data.");
                        break;

                    default:
                        EMSFrm.Navigate(eMSDashboard);
                        LogService.AddLog("Unknown EMS inner navigation target. Loaded EMS dashboard.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"EMS inner navigation failed. Target:{tag}", ex);
                MessageBox.Show(ex.Message, "EMS Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
