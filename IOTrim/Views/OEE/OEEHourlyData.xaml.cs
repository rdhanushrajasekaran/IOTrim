using IOTrim.Model;
using IOTrim.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace IOTrim.Views
{
    public partial class OEEHourlyData : Page
    {
        private readonly string connectStr =
            @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True";

        private ObservableCollection<OeeHourlyModel> _allData = new();

        public OEEHourlyData()
        {
            InitializeComponent();
            Loaded += OEEHourlyData_Loaded;
        }

        private void OEEHourlyData_Loaded(object sender, RoutedEventArgs e)
        {
            DatePicker.SelectedDateTime = DateTime.Today;
            LoadDataForDate(DateTime.Today);
        }

        private void DateSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            DateTime selectedDate = DatePicker.SelectedDateTime?.Date ?? DateTime.Today;
            LoadDataForDate(selectedDate);
        }

        private void LoadDataForDate(DateTime selectedDate)
        {
            try
            {
                LogService.AddLog($"Loading OEE hourly data for date: {selectedDate:dd-MMM-yyyy}");

                ObservableCollection<OeeHourlyModel> list = new ObservableCollection<OeeHourlyModel>();

                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();

                    string query = @"
                        WITH HourlyLatest AS
                        (
                            SELECT 
                                LogDateTime,
                                RunTime,
                                ManagementLoss,
                                Idle,
                                PlannedDowntime,
                                UnplannedDowntime,
                                GoodPart,
                                BadPart,
                                A,
                                P,
                                Q,
                                OEE,
                                ROW_NUMBER() OVER
                                (
                                    PARTITION BY DATEPART(HOUR, LogDateTime)
                                    ORDER BY LogDateTime DESC
                                ) AS RowNo
                            FROM OEELog
                            WHERE LogDateTime >= @StartDate
                              AND LogDateTime < @EndDate
                        )
                        SELECT *
                        FROM HourlyLatest
                        WHERE RowNo = 1
                        ORDER BY LogDateTime ASC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", selectedDate.Date);
                        cmd.Parameters.AddWithValue("@EndDate", selectedDate.Date.AddDays(1));

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            int sno = 1;
                            

                            while (reader.Read())
                            {
                                DateTime logDateTime = Convert.ToDateTime(reader["LogDateTime"]);
                                DateTime hourStart = new DateTime(
                            logDateTime.Year,
                            logDateTime.Month,
                            logDateTime.Day,
                            logDateTime.Hour,
                            0,
                            0);
                                list.Add(new OeeHourlyModel
                                {
                                    SNo = sno.ToString(),
                                    HourValue = logDateTime.Hour,
                                    Hour = $"{hourStart:HH:mm} - {hourStart.AddHours(1):HH:mm}",

                                    RunTime = reader["RunTime"].ToString(),
                                    ManagementLoss = reader["ManagementLoss"].ToString(),
                                    Idle = reader["Idle"].ToString(),
                                    PlannedDowntime = reader["PlannedDowntime"].ToString(),
                                    UnplannedDowntime = reader["UnplannedDowntime"].ToString(),
                                    GoodPart = reader["GoodPart"].ToString(),
                                    BadPart = reader["BadPart"].ToString(),

                                    A = reader["A"].ToString(),
                                    P = reader["P"].ToString(),
                                    Q = reader["Q"].ToString(),
                                    OEE = reader["OEE"].ToString()
                                });

                                sno++;
                            }
                        }
                    }
                }

                _allData = list;
                dgOeeHourly.ItemsSource = _allData;
            }
            catch (Exception ex)
            {
                LogService.AddException("OEE hourly data load failed", ex);
                MessageBox.Show(ex.Message, "OEE Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                IEnumerable<OeeHourlyModel> filtered = _allData;

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

                dgOeeHourly.ItemsSource = new ObservableCollection<OeeHourlyModel>(filtered);
            }
            catch (Exception ex)
            {
                LogService.AddException("Hourly sorting change failed", ex);
                MessageBox.Show(ex.Message, "Sorting Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Export_Data(dgOeeHourly,"OEE");
        }
    }
}