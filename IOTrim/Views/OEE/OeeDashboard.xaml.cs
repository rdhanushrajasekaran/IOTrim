using IOTrim.Service;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace IOTrim.Views
{
    public partial class OeeDashboard : Page, INotifyPropertyChanged
    {
        private double oeeValue = 50;
        private string TagOPC = "ns=4;s=|var|AX-564EB0MB1T.Application.Production.";

        public double OEEValue
        {
            get => oeeValue;
            set
            {
                if (EqualityComparer<double>.Default.Equals(oeeValue, value)) return;
                oeeValue = value;
                OnPropertyChanged(nameof(OEEValue));
                OnPropertyChanged(nameof(OEEValueText));
                OnPropertyChanged(nameof(OEEPercentText));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public DispatcherTimer refreshTimer { get; set; }

        public string OEEValueText => $"{OEEValue:0.00}";
        public string OEEPercentText => $"{OEEValue:0.00} %";

        private ISeries[] gaugeSeries;
        public ISeries[] GaugeSeries
        {
            get => gaugeSeries;
            set
            {
                gaugeSeries = value;
                OnPropertyChanged(nameof(GaugeSeries));
            }
        }

        #region Time Details

        private double totalTimeMinutes;
        private double totalAvailableTimeMinutes;
        private double shiftBreakTimeMinutes;
        private double actualRunningTimeMinutes;
        private double managementLossTimeMinutes;
        private double plannedDowntimeMinutes;
        private double unplannedDowntimeMinutes;
        private double idleTimeMinutes;

        private string selectedTimeUnit = "min";
        public string SelectedTimeUnit
        {
            get => selectedTimeUnit;
            set
            {
                if (selectedTimeUnit == value) return;

                selectedTimeUnit = value;
                OnPropertyChanged(nameof(SelectedTimeUnit));
                RefreshTimeTexts();
            }
        }

        // Getter-only properties calculate values automatically based on the double metrics
        public string TotalTime => ConvertTime(totalTimeMinutes);
        public string TotalAvailableTime => ConvertTime(totalAvailableTimeMinutes);
        public string ShiftBreakTime => ConvertTime(shiftBreakTimeMinutes);
        public string ActualRunningTime => ConvertTime(actualRunningTimeMinutes);
        public string ManagementLossTime => ConvertTime(managementLossTimeMinutes);
        public string PlannedDowntime => ConvertTime(plannedDowntimeMinutes);
        public string UnplannedDowntime => ConvertTime(unplannedDowntimeMinutes);
        public string IdleTime => ConvertTime(idleTimeMinutes);

        private string ConvertTime(double minutes)
        {
            return SelectedTimeUnit switch
            {
                "sec" => $"{minutes * 60:0.##} sec",
                "hour" => $"{minutes / 60:0.##} hr",
                _ => $"{minutes:0.##} min"
            };
        }

        private void RefreshTimeTexts()
        {
            OnPropertyChanged(nameof(TotalTime));
            OnPropertyChanged(nameof(TotalAvailableTime));
            OnPropertyChanged(nameof(ShiftBreakTime));
            OnPropertyChanged(nameof(ActualRunningTime));
            OnPropertyChanged(nameof(ManagementLossTime));
            OnPropertyChanged(nameof(PlannedDowntime));
            OnPropertyChanged(nameof(UnplannedDowntime));
            OnPropertyChanged(nameof(IdleTime));
        }

        #endregion

        #region Parts Summary

        private string goodParts;
        public string GoodParts
        {
            get => goodParts;
            set
            {
                goodParts = value;
                OnPropertyChanged(nameof(GoodParts));
            }
        }

        private string badParts;
        public string BadParts
        {
            get => badParts;
            set
            {
                badParts = value;
                OnPropertyChanged(nameof(BadParts));
            }
        }

        private string totalParts;
        public string TotalParts
        {
            get => totalParts;
            set
            {
                totalParts = value;
                OnPropertyChanged(nameof(TotalParts));
            }
        }

        #endregion

        #region KPI Progress Bars

        private double availability;
        public double Availability
        {
            get => availability;
            set
            {
                if (EqualityComparer<double>.Default.Equals(availability, value)) return;
                availability = value;
                OnPropertyChanged(nameof(Availability));
                OnPropertyChanged(nameof(AvailabilityText));
            }
        }
        public string AvailabilityText => $"{Availability:0.##}%";

        private double performance;
        public double Performance
        {
            get => performance;
            set
            {
                if (EqualityComparer<double>.Default.Equals(performance, value)) return;
                performance = value;
                OnPropertyChanged(nameof(Performance));
                OnPropertyChanged(nameof(PerformanceText));
            }
        }
        public string PerformanceText => $"{Performance:0.##}%";

        private double quality;
        public double Quality
        {
            get => quality;
            set
            {
                if (EqualityComparer<double>.Default.Equals(quality, value)) return;
                quality = value;
                OnPropertyChanged(nameof(Quality));
                OnPropertyChanged(nameof(QualityText));
            }
        }
        public string QualityText => $"{Quality:0.##}%";

        #endregion

        public OeeDashboard()
        {
            InitializeComponent();
            DataContext = this;
            LoadOEEDashBoardData();
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromSeconds(5);
            refreshTimer.Tick += (_, _) => LoadOEEDashBoardData();
            refreshTimer.Start();
        }

        private void LoadOEEDashBoardData()
        {
            try
            {
                LoadLatestValues();
            }
            catch (Exception ex)
            {
                LogService.AddLog($"Error in LoadOEEDashBoardData: {ex.Message}");
            }
        }

        private void LoadLatestValues()
        {
            using SqlConnection connection = new SqlConnection(modGlobal.connectStr);
            connection.Open();

            string query = "SELECT TOP 1 * FROM OEELog ORDER BY ID DESC";

            using SqlCommand cmd = new SqlCommand(query, connection);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                applyLatestReader(reader);
            }
        }

        private void applyLatestReader(SqlDataReader reader)
        {
            // 1. Assign values to the underlying private metric double fields instead of writing to read-only strings
            managementLossTimeMinutes = ReadFloatTag($"{TagOPC}Mgmt_Loss_Time");
            plannedDowntimeMinutes = ReadFloatTag($"{TagOPC}Planned_Downtime");
            unplannedDowntimeMinutes = ReadFloatTag($"{TagOPC}Unplanned_Downtime");
            idleTimeMinutes = ReadFloatTag($"{TagOPC}Idle_Time");
            actualRunningTimeMinutes = ReadFloatTag($"{TagOPC}Act_Running_Time");
            totalTimeMinutes = ReadFloatTag($"{TagOPC}Total_Time");
            totalAvailableTimeMinutes = ReadFloatTag($"{TagOPC}Avail_Time");

            // 2. Refresh the public time strings via INotifyPropertyChanged so WPF updates
            RefreshTimeTexts();

            // Parts counters remain string-driven
            float good = ReadFloatTag($"{TagOPC}Good_Parts");
            float bad = ReadFloatTag($"{TagOPC}Bad_Parts");
            float totalPartsValue = ReadFloatTag($"{TagOPC}Total_Parts");

            GoodParts = $"{good}";
            BadParts = $"{bad}";
            TotalParts = $"{totalPartsValue}";

            // KPI Progress Bars
            Availability = ReadFloatTag($"{TagOPC}Availability");
            Performance = ReadFloatTag($"{TagOPC}Performance");
            Quality = ReadFloatTag($"{TagOPC}Quality");

            // Central gauge value
            OEEValue = ReadFloatTag($"{TagOPC}OEE");
            UpdateGaugeSeries(OEEValue);
        }

        private void UpdateGaugeSeries(double value)
        {
            GaugeSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Name= "OEE",
                    Values = new double[] { value },
                    InnerRadius = 85,
                    Fill = new SolidColorPaint(SKColor.Parse("#00A6D6")),
                    Stroke = null
                },
                new PieSeries<double>
                {
                    Name = "Remaining",
                    Values = new double[] { 100 - value },
                    InnerRadius = 85,
                    Fill = new SolidColorPaint(SKColor.Parse("#DC2626")),
                    Stroke = null
                }
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private float ReadFloatTag(string nodeId)
        {
            object? value = AppServices.OpcUaService.ReadValue(nodeId);
            if (value == null || value == DBNull.Value)
                return 0f;

            return Convert.ToSingle(value);
        }

        private static double ReadDouble(SqlDataReader reader, string columnName)
        {
            try
            {
                int ordinal = reader.GetOrdinal(columnName);
                return reader.IsDBNull(ordinal) ? 0 : Convert.ToDouble(reader.GetValue(ordinal));
            }
            catch (Exception ex)
            {
                LogService.AddException($"EMS dashboard numeric read failed. Column:{columnName}", ex);
                return 0;
            }
        }

        private void TimeUnit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;

            int index = Convert.ToInt32(button.Tag);
            string selectedUnit = button.Content.ToString();

            SelectedTimeUnit = selectedUnit;

            double targetX = index * 60;

            DoubleAnimation animation = new DoubleAnimation
            {
                To = targetX,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            TimeUnitIndicatorTransform.BeginAnimation(TranslateTransform.XProperty, animation);

            BtnSec.Foreground = Brushes.Black;
            BtnMin.Foreground = Brushes.Black;
            BtnHour.Foreground = Brushes.Black;

            button.Foreground = Brushes.White;
        }
    }
}