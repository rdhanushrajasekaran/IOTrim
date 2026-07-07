using ClosedXML.Excel;
using IOTrim.Service;
using Microsoft.Win32;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace IOTrim.Views
{
    public partial class DataPage : Page
    {
        private readonly string connectStr = @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True";

        public DataPage()
        {
            InitializeComponent();
            Loaded += DataPage_Loaded;
        }

        private void DataPage_Loaded(object sender, RoutedEventArgs e)
        {
            LogService.AddLog("Data page loaded. Loading production log data.");
            LoadAllProductionLogs();
        }

        private void LoadAllProductionLogs()
        {
            try
            {
                LogService.AddLog("Database load started: ProductionLog all records.");

                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();
                    LogService.AddLog("Database connection opened for full production log load.");

                    string query = "SELECT * FROM ProductionLog ORDER BY DateTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        LoadGridFromCommand(cmd, "Full production log load");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Production log full load failed", ex);
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DateSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.AddLog("Date search button clicked.");

                if (!FromDatePicker.SelectedDateTime.HasValue || !ToDatePicker.SelectedDateTime.HasValue)
                {
                    LogService.AddLog("Date search stopped. From date or To date not selected.");
                    MessageBox.Show("Please select both From and To dates.", "Date Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime fromDate = FromDatePicker.SelectedDateTime.Value;
                DateTime toDate = ToDatePicker.SelectedDateTime.Value;

                if (!DateValidation(fromDate, toDate))
                    return;

                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();
                    LogService.AddLog($"Database connection opened for date search. From:{fromDate:yyyy-MM-dd HH:mm:ss} To:{toDate:yyyy-MM-dd HH:mm:ss}");

                    string query = @"
                        SELECT *
                        FROM ProductionLog
                        WHERE DateTime BETWEEN @FromDate AND @ToDate
                        ORDER BY DateTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FromDate", fromDate);
                        cmd.Parameters.AddWithValue("@ToDate", toDate);

                        LoadGridFromCommand(cmd, "Date search");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Date search failed", ex);
                MessageBox.Show(ex.Message, "Date Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool DateValidation(DateTime fromDate, DateTime toDate)
        {
            if (fromDate > toDate)
            {
                MessageBox.Show(
                    "From Date cannot be greater than To Date.",
                    "Date Validation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                LogService.AddLog($"Invalid date range. From:{fromDate:dd-MM-yyyy HH:mm:ss} To:{toDate:dd-MM-yyyy HH:mm:ss}");
                return false;
            }

            LogService.AddLog($"Date range validated. From:{fromDate:dd-MM-yyyy HH:mm:ss} To:{toDate:dd-MM-yyyy HH:mm:ss}");
            return true;
        }

        private void HolderSerialSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string barcode = HolderSerialTxtBox.Text.Trim();
                LogService.AddLog($"Holder serial number search button clicked. HolderSerialNumber:{barcode}");

                if (string.IsNullOrWhiteSpace(barcode))
                {
                    LogService.AddLog("Holder serial number search stopped. Empty Holder serial number value.");
                    MessageBox.Show("Please enter Holder serial number value.", "Holder serial number Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();
                    LogService.AddLog("Database connection opened for Holder serial number search.");

                    string query = @"SELECT * FROM ProductionLog WHERE HolderSerialNo=@Barcode ORDER BY DateTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        LoadGridFromCommand(cmd, "Holder serial number search");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Holder serial number search failed", ex);
                MessageBox.Show(ex.Message, "Holder serial number Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void blSerailSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string barcode = blSerialNoTxtBox.Text.Trim();
                LogService.AddLog($"BL serial search button clicked. BLSerialNo:{barcode}");

                if (string.IsNullOrWhiteSpace(barcode))
                {
                    LogService.AddLog("BL serial search stopped. Empty BL serial value.");
                    MessageBox.Show("Please enter BL Serial No.", "BL Serial Search", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();
                    LogService.AddLog("Database connection opened for BL serial search.");

                    string query = @"SELECT * FROM ProductionLog WHERE BLSerialNo=@Barcode ORDER BY DateTime DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Barcode", barcode);
                        LoadGridFromCommand(cmd, "BL serial search");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("BL serial search failed", ex);
                MessageBox.Show(ex.Message, "BL Serial Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadGridFromCommand(SqlCommand cmd, string operationName)
        {
            DataTable dt = new DataTable();

            using (SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(cmd))
            {
                sqlDataAdapter.Fill(dt);
            }

            dgproductionLog.ItemsSource = null;
            dgproductionLog.ItemsSource = dt.DefaultView;

            LogService.AddLog($"{operationName} completed. Rows loaded:{dt.Rows.Count}");
        }

        private void Soring_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (dgproductionLog.ItemsSource == null || cbSorting.SelectedItem == null)
                    return;

                string selectedItem = ((ComboBoxItem)cbSorting.SelectedItem).Content?.ToString() ?? "All";

                if (dgproductionLog.ItemsSource is not DataView dv)
                {
                    LogService.AddLog("Sorting/filter stopped. DataGrid ItemsSource is not DataView.");
                    return;
                }

                switch (selectedItem)
                {
                    case "PVD":
                        dv.RowFilter = "Variant = 'PVD'";
                        break;

                    case "Non PVD":
                        dv.RowFilter = "Variant = 'Non PVD'";
                        break;

                    case "OK":
                        dv.RowFilter = "Result = 'OK'";
                        break;

                    case "NOK":
                        dv.RowFilter = "Result = 'NG'";
                        break;

                    case "Black":
                        dv.RowFilter = "Color = 'Black'";
                        break;

                    case "Brown":
                        dv.RowFilter = "Color = 'Brown'";
                        break;

                    case "All":
                    default:
                        dv.RowFilter = "";
                        break;
                }

                LogService.AddLog($"Grid filter changed. Filter:{selectedItem}, Visible rows:{dv.Count}");
            }
            catch (Exception ex)
            {
                LogService.AddException("Grid filter failed", ex);
                MessageBox.Show(ex.Message, "Filter Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            AppServices.Export_Data(dgproductionLog, "ProductionLog");
        }
    }
}
