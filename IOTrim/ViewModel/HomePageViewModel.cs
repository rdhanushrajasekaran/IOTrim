using IOTrim.Service;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Documents;
using System.Windows.Threading;

namespace IOTrim.ViewModel
{
    public class HomeViewModel : INotifyPropertyChanged
    {
        private readonly DispatcherTimer refreshTimer;

        private double oeeValue;
        private int okCount;
        private int ngCount;
        private int totalCount;
        private double targetProgress;
        private string targetProgressText = "0%";
        private string oeeStatus = "Checking";
        private string currentShift = "-";
        private string shiftTimeText = "-";
        private string currentVariant = "-";
        private string availabilityText = "Availability  0%";
        private string performanceText = "Performance  0%";
        private string qualityText = "Quality  0%";
        private double availabilityValue;
        private double performanceValue;
        private double qualityValue;

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsConnected => modGlobal.IsPLCConnected;

        private bool _isOEEConnected;
        public bool IsOEEConnected
        {
            get => _isOEEConnected;
            set
            {
                if (_isOEEConnected == value) return;

                _isOEEConnected = value;
                OnPropertyChanged(nameof(IsOEEConnected));
            }
        }

        public string MachineStatusText => IsConnected ? "Running" : "Waiting";

        public double OEEValue
        {
            get => oeeValue;
            set { oeeValue = value; OnPropertyChanged(nameof(OEEValue)); OnPropertyChanged(nameof(OEEValueText)); }
        }

        public string OEEValueText => $"{OEEValue:0}";

        public int OKCount
        {
            get => okCount;
            set { okCount = value; OnPropertyChanged(nameof(OKCount)); }
        }

        public int NGCount
        {
            get => ngCount;
            set { ngCount = value; OnPropertyChanged(nameof(NGCount)); }
        }

        public int TotalCount
        {
            get => totalCount;
            set { totalCount = value; OnPropertyChanged(nameof(TotalCount)); }
        }

        public double TargetProgress
        {
            get => targetProgress;
            set { targetProgress = value; OnPropertyChanged(nameof(TargetProgress)); }
        }

        public string TargetProgressText
        {
            get => targetProgressText;
            set { targetProgressText = value; OnPropertyChanged(nameof(TargetProgressText)); }
        }

        public string OEEStatus
        {
            get => oeeStatus;
            set { oeeStatus = value; OnPropertyChanged(nameof(OEEStatus)); }
        }

        public string CurrentShift
        {
            get => currentShift;
            set { currentShift = value; OnPropertyChanged(nameof(CurrentShift)); }
        }

        public string ShiftTimeText
        {
            get => shiftTimeText;
            set { shiftTimeText = value; OnPropertyChanged(nameof(ShiftTimeText)); }
        }

        public string CurrentVariant
        {
            get => currentVariant;
            set { currentVariant = value; OnPropertyChanged(nameof(CurrentVariant)); }
        }

        public string AvailabilityText
        {
            get => availabilityText;
            set { availabilityText = value; OnPropertyChanged(nameof(AvailabilityText)); }
        }

        public string PerformanceText
        {
            get => performanceText;
            set { performanceText = value; OnPropertyChanged(nameof(PerformanceText)); }
        }

        public string QualityText
        {
            get => qualityText;
            set { qualityText = value; OnPropertyChanged(nameof(QualityText)); }
        }

        public double AvailabilityValue
        {
            get => availabilityValue;
            set { availabilityValue = value; OnPropertyChanged(nameof(AvailabilityValue)); }
        }

        public double PerformanceValue
        {
            get => performanceValue;
            set { performanceValue = value; OnPropertyChanged(nameof(PerformanceValue)); }
        }

        public double QualityValue
        {
            get => qualityValue;
            set { qualityValue = value; OnPropertyChanged(nameof(QualityValue)); }
        }

        public ISeries[] GaugeSeries { get; private set; } = Array.Empty<ISeries>();
        public ISeries[] ProductionSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] ProductionXAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] ProductionYAxes { get; private set; } = Array.Empty<Axis>();

        public HomeViewModel()
        {
            modGlobal.PLCConnectionChanged += () =>
            {
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(MachineStatusText));
            };
            LoadDashboardData();
            refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(10)
            };
            refreshTimer.Tick += (_, _) => LoadDashboardData();
            refreshTimer.Start();
        }

        private void LoadDashboardData()
        {
            try
            {
                IsOEEConnected = AppServices.OpcUaService.IsConnected;
                UpdateShiftInfo();
                UpdateTodayProductionCount();
                UpdateProductionTrend();
                UpdateOeeValues();
            }
            catch (Exception ex)
            {
                OEEStatus = "Error";
                LogService.AddException("Home dashboard refresh failed", ex);
            }
        }

        private void UpdateShiftInfo()
        {
            TimeSpan now = DateTime.Now.TimeOfDay;

            if (now >= new TimeSpan(6, 0, 0) && now < new TimeSpan(14, 0, 0))
            {
                CurrentShift = "A Shift";
                ShiftTimeText = "06:30 AM - 02:30 PM";
            }
            else if (now >= new TimeSpan(14, 0, 0) && now < new TimeSpan(22, 0, 0))
            {
                CurrentShift = "B Shift";
                ShiftTimeText = "02:30 PM - 10:30 PM";
            }
            else
            {
                CurrentShift = "C Shift";
                ShiftTimeText = "10:30 PM - 06:30 AM";
            }
        }

        private void UpdateTodayProductionCount()
        {
            DateTime todayDate = DateTime.Today;
            OKCount = 0;
            NGCount = 0;

            using SqlConnection conn = new SqlConnection(modGlobal.connectStr);
            conn.Open();

            string query = @"SELECT Result, COUNT(*) AS TotalCount
                             FROM ProductionLog
                             WHERE DateTime >= @TodayDate
                               AND DateTime < DATEADD(DAY, 1, @TodayDate)
                               AND Result IS NOT NULL
                             GROUP BY Result;

                             SELECT TOP 1 Variant
                             FROM ProductionLog
                             WHERE DateTime >= @TodayDate
                               AND DateTime < DATEADD(DAY, 1, @TodayDate)
                             ORDER BY DateTime DESC;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TodayDate", todayDate);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string result = Convert.ToString(reader["Result"]) ?? string.Empty;
                int count = Convert.ToInt32(reader["TotalCount"]);

                if (result.Equals("OK", StringComparison.OrdinalIgnoreCase)) OKCount += count;
                else if (result.Equals("NG", StringComparison.OrdinalIgnoreCase) || result.Equals("NOK", StringComparison.OrdinalIgnoreCase) || result.Equals("KO", StringComparison.OrdinalIgnoreCase)) NGCount += count;
            }

            TotalCount = OKCount + NGCount;

            if (reader.NextResult() && reader.Read())
            {
                CurrentVariant = Convert.ToString(reader["Variant"]) ?? "-";
            }

            const double dailyTarget = 1500.0;
            TargetProgress = Math.Min(100.0, TotalCount / dailyTarget * 100.0);
            TargetProgressText = $"{TargetProgress:0}%";
        }


        private void UpdateProductionTrend()
        {
            DateTime todayDate = DateTime.Today;
           // int[] okValues = new int[24];
           // int[] ngValues = new int[24];

            using SqlConnection conn = new SqlConnection(modGlobal.connectStr);
            conn.Open();

            string query = @"SELECT DATEPART(HOUR, DateTime) AS HourValue,
                                    SUM(CASE WHEN Result = 'OK' THEN 1 ELSE 0 END) AS OKCount,
                                    SUM(CASE WHEN Result IN ('NG', 'NOK', 'KO') THEN 1 ELSE 0 END) AS NGCount
                             FROM ProductionLog
                             WHERE DateTime >= @TodayDate
                               AND DateTime < DATEADD(DAY, 1, @TodayDate)
                             GROUP BY DATEPART(HOUR, DateTime)
                             ORDER BY HourValue;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@TodayDate", todayDate);

            using SqlDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int hour = Convert.ToInt32(reader["HourValue"]);
                if (hour < 0 || hour > 23) continue;

               // okValues[hour] = reader["OKCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["OKCount"]);
               // ngValues[hour] = reader["NGCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["NGCount"]);
            }

            int[] okValues =
{
    40, 48, 55, 62, 75, 90,
    115, 145, 175, 205, 225, 245,
    260, 275, 290, 300, 295, 285,
    265, 240, 215, 185, 150, 120
};

            int[] ngValues =
            {
     8, 10, 12, 15, 14, 18,
    20, 22, 25, 28, 30, 32,
    35, 33, 30, 28, 26, 24,
    22, 20, 18, 15, 12, 10
};

            var textColor =modGlobal.GetThemeColor("AppTextBrush");

            int[] totalValues = okValues
                .Zip(ngValues, (ok, ng) => ok + ng)
                .ToArray();

            ProductionSeries = new ISeries[]
            {
                new StackedColumnSeries<int>
                {
                    Name = "NG",
                    Values = ngValues,
                    Fill = new SolidColorPaint(SKColor.Parse("#de5252")),
                    Stroke = null,
                    MaxBarWidth = 35
                },
                new StackedColumnSeries<int>
                {
                    Name = "OK",
                    Values = okValues,
                    Fill = new SolidColorPaint(SKColor.Parse("#22C55E")),
                    Stroke = null,
                    MaxBarWidth = 35
                },

                new LineSeries<int>
                {
                    Name = "Total Trend",
                    Values = totalValues,
                    GeometrySize = 7,
                    Stroke = new SolidColorPaint(SKColor.Parse("#2563EB"), 3),
                    Fill = null,
                    LineSmoothness = 0.85
                }
            };

            ProductionXAxes = new Axis[]
            {
                new Axis
                {
                    Labels = Enumerable.Range(0, 24)
                        .Select(i => $"{i}-{(i + 1) % 24}")
                        .ToArray(),

                    TextSize = 16,
                    LabelsRotation = 0,
                    MinStep = 1
                }
            };


            OnPropertyChanged(nameof(ProductionSeries));
            OnPropertyChanged(nameof(ProductionXAxes));
            OnPropertyChanged(nameof(ProductionYAxes));
        }

        private void UpdateOeeValues()
        {
            double quality = TotalCount == 0 ? 0 : OKCount * 100.0 / TotalCount;
            double performance = Math.Min(100.0, TargetProgress);
            double availability = TotalCount > 0 ? 100.0 : 0.0;
            double oee = availability * performance * quality / 10000.0;

            AvailabilityValue = availability;
            PerformanceValue = performance;
            QualityValue = quality;
            OEEValue = oee;

            AvailabilityText = $"Availability  {availability:0}%";
            PerformanceText = $"Performance  {performance:0}%";
            QualityText = $"Quality  {quality:0}%";

            UpdateGaugeSeries(OEEValue);

            OnPropertyChanged(nameof(GaugeSeries));
        }
        private void UpdateGaugeSeries(double value)
        {
            GaugeSeries = new ISeries[]
            {
                new PieSeries<double>
                {
                    Name = "OEE",
                    Values = new double[] { value },
                    InnerRadius = 110,
                    Fill = new SolidColorPaint(SKColor.Parse("#2563EB")),
                    Stroke = null,

                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:0.00}%"
                },

                new PieSeries<double>
                {
                    Name = "Remaining",
                    Values = new double[] { 100 - value },
                    InnerRadius = 110,
                    Fill = new SolidColorPaint(SKColor.Parse("#E5E7EB")),
                    Stroke = null,

                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:0.00}%"
                }
            };
        }
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
