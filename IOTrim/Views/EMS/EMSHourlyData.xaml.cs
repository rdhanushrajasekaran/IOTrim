using IOTrim.Model;
using IOTrim.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IOTrim.Views.EMS
{
    /// <summary>
    /// Interaction logic for EMSHourlyData.xaml
    /// </summary>
    public partial class EMSHourlyData : Page
    {

        private ObservableCollection<EMSModel> _allData = new();
        public EMSHourlyData()
        {
            InitializeComponent();
        }

        private void DateSearchBtn_Click(object sender, RoutedEventArgs e)
        {

        }


        private void Sorting_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (_allData == null || _allData.Count == 0)
                    return;

                string selectedSorting =
                    (cbSorting.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;

                if (string.IsNullOrEmpty(selectedSorting))
                    return;

                IEnumerable<EMSModel> filtered = _allData;

                switch (selectedSorting)
                {
                    case "Shift A":
                        filtered = _allData.Where(x => x.HourValue >= 6 && x.HourValue < 14);
                        break;

                    case "Shift B":
                        filtered = _allData.Where(x => x.HourValue >= 14 && x.HourValue < 22);
                        break;

                    case "Shift C":
                        filtered = _allData.Where(x => x.HourValue >= 22 || x.HourValue < 6);
                        break;

                    case "All":
                        filtered = _allData;
                        break;
                }

                dgEMSHourly.ItemsSource = new ObservableCollection<EMSModel>(filtered);
            }
            catch (Exception ex)
            {
                LogService.AddException("Hourly sorting change failed", ex);
                MessageBox.Show(ex.Message, "Sorting Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Export_Data(dgEMSHourly,"EMS");
        }
    }
}
