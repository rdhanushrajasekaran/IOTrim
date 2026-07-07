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
using System.Windows.Controls;
using System.Windows.Threading;

namespace IOTrim.Views.EMS
{
    public partial class EMSDashboard : Page, INotifyPropertyChanged
    {
        private readonly DispatcherTimer refreshTimer;

        private string totalEnergyText = "0 kWh";
        private string activePowerText = "0 kW";
        private string todayEnergyText = "0 kWh";
        private string apparentPowerText = "0 kVA";
        private string l1L2VoltText = "0 V";
        private string l2L3VoltText = "0 V";
        private string l3L1VoltText = "0 V";
        private double l1L2VoltPercent;
        private double l2L3VoltPercent;
        private double l3L1VoltPercent;
        private string l1CurrentText = "0 A";
        private string l2CurrentText = "0 A";
        private string l3CurrentText = "0 A";
        private string l1VoltText = "0 V";
        private string l2VoltText = "0 V";
        private string l3VoltText = "0 V";
        private string voltageBalanceStatus = "No data";
        private string powerFactorText = "0.00";
        private string frequencyText = "0 Hz";
        private string reactivePowerText = "0 kVAr";
        private string aShiftEnergyText = "A Shift  0 kWh";
        private string bShiftEnergyText = "B Shift  0 kWh";
        private string cShiftEnergyText = "C Shift  0 kWh";
        private double aShiftEnergyValue;
        private double bShiftEnergyValue;
        private double cShiftEnergyValue;
        private string monthlyEnergyText = "0 kWh";
        private string averageDayEnergyText = "0 kWh";
        private string warningText = "No active warning";

        public event PropertyChangedEventHandler? PropertyChanged;

        public ISeries[] PowerSeries { get; private set; } = Array.Empty<ISeries>();
        public Axis[] PowerXAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] PowerYAxes { get; private set; } = Array.Empty<Axis>();

        public string TotalEnergyText { get => totalEnergyText; set { totalEnergyText = value; OnPropertyChanged(nameof(TotalEnergyText)); } }
        public string ActivePowerText { get => activePowerText; set { activePowerText = value; OnPropertyChanged(nameof(ActivePowerText)); } }
        public string TodayEnergyText { get => todayEnergyText; set { todayEnergyText = value; OnPropertyChanged(nameof(TodayEnergyText)); } }
        public string ApparentPowerText { get => apparentPowerText; set { apparentPowerText = value; OnPropertyChanged(nameof(ApparentPowerText)); } }
        public string L1L2VoltText { get => l1L2VoltText; set { l1L2VoltText = value; OnPropertyChanged(nameof(L1L2VoltText)); } }
        public string L2L3VoltText { get => l2L3VoltText; set { l2L3VoltText = value; OnPropertyChanged(nameof(L2L3VoltText)); } }
        public string L3L1VoltText { get => l3L1VoltText; set { l3L1VoltText = value; OnPropertyChanged(nameof(L3L1VoltText)); } }
        public double L1L2VoltPercent { get => l1L2VoltPercent; set { l1L2VoltPercent = value; OnPropertyChanged(nameof(L1L2VoltPercent)); } }
        public double L2L3VoltPercent { get => l2L3VoltPercent; set { l2L3VoltPercent = value; OnPropertyChanged(nameof(L2L3VoltPercent)); } }
        public double L3L1VoltPercent { get => l3L1VoltPercent; set { l3L1VoltPercent = value; OnPropertyChanged(nameof(L3L1VoltPercent)); } }
        public string L1CurrentText { get => l1CurrentText; set { l1CurrentText = value; OnPropertyChanged(nameof(L1CurrentText)); } }
        public string L2CurrentText { get => l2CurrentText; set { l2CurrentText = value; OnPropertyChanged(nameof(L2CurrentText)); } }
        public string L3CurrentText { get => l3CurrentText; set { l3CurrentText = value; OnPropertyChanged(nameof(L3CurrentText)); } }
        public string L1VoltText { get => l1VoltText; set { l1VoltText = value; OnPropertyChanged(nameof(L1VoltText)); } }
        public string L2VoltText { get => l2VoltText; set { l2VoltText = value; OnPropertyChanged(nameof(L2VoltText)); } }
        public string L3VoltText { get => l3VoltText; set { l3VoltText = value; OnPropertyChanged(nameof(L3VoltText)); } }
        public string VoltageBalanceStatus { get => voltageBalanceStatus; set { voltageBalanceStatus = value; OnPropertyChanged(nameof(VoltageBalanceStatus)); } }
        public string PowerFactorText { get => powerFactorText; set { powerFactorText = value; OnPropertyChanged(nameof(PowerFactorText)); } }
        public string FrequencyText { get => frequencyText; set { frequencyText = value; OnPropertyChanged(nameof(FrequencyText)); } }
        public string ReactivePowerText { get => reactivePowerText; set { reactivePowerText = value; OnPropertyChanged(nameof(ReactivePowerText)); } }
        public string AShiftEnergyText { get => aShiftEnergyText; set { aShiftEnergyText = value; OnPropertyChanged(nameof(AShiftEnergyText)); } }
        public string BShiftEnergyText { get => bShiftEnergyText; set { bShiftEnergyText = value; OnPropertyChanged(nameof(BShiftEnergyText)); } }
        public string CShiftEnergyText { get => cShiftEnergyText; set { cShiftEnergyText = value; OnPropertyChanged(nameof(CShiftEnergyText)); } }
        public double AShiftEnergyValue { get => aShiftEnergyValue; set { aShiftEnergyValue = value; OnPropertyChanged(nameof(AShiftEnergyValue)); } }
        public double BShiftEnergyValue { get => bShiftEnergyValue; set { bShiftEnergyValue = value; OnPropertyChanged(nameof(BShiftEnergyValue)); } }
        public double CShiftEnergyValue { get => cShiftEnergyValue; set { cShiftEnergyValue = value; OnPropertyChanged(nameof(CShiftEnergyValue)); } }
        public string MonthlyEnergyText { get => monthlyEnergyText; set { monthlyEnergyText = value; OnPropertyChanged(nameof(MonthlyEnergyText)); } }
        public string AverageDayEnergyText { get => averageDayEnergyText; set { averageDayEnergyText = value; OnPropertyChanged(nameof(AverageDayEnergyText)); } }
        public string WarningText { get => warningText; set { warningText = value; OnPropertyChanged(nameof(WarningText)); } }

        public EMSDashboard()
        {
            InitializeComponent();
            DataContext = this;

            LoadEmsDashboardData();

            refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            refreshTimer.Tick += (_, _) => LoadEmsDashboardData();
            refreshTimer.Start();
        }

        private void LoadEmsDashboardData()
        {
            try
            {
                LoadLatestValues();
                LoadPowerTrend();
                LoadMonthEnergy();
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS dashboard refresh failed", ex);
            }
        }

        private void LoadLatestValues()
        {
            using SqlConnection conn = new SqlConnection(modGlobal.connectStr);
            conn.Open();

            string query = @"SELECT TOP 1 * FROM EnergyMeterTemp ORDER BY LogTime DESC;
                             SELECT TOP 1 * FROM EnergyMeterLog ORDER BY [Hour] DESC;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            bool loaded = false;
            if (reader.Read())
            {
                ApplyLatestReader(reader);
                loaded = true;
            }

            if (!loaded && reader.NextResult() && reader.Read())
            {
                ApplyLatestReader(reader);
            }
        }

        private void ApplyLatestReader(SqlDataReader reader)
        {
            double l12 = ReadDouble(reader, "L1_L2_Volt");
            double l23 = ReadDouble(reader, "L2_L3_Volt");
            double l31 = ReadDouble(reader, "L3_L1_Volt");
            double c1 = ReadDouble(reader, "L1_C");
            double c2 = ReadDouble(reader, "L2_C");
            double c3 = ReadDouble(reader, "L3_C");

            double vln1 = ReadDouble(reader, "L1_N_Volt");
            double vln2 = ReadDouble(reader, "L2_N_Volt");
            double vln3 = ReadDouble(reader, "L3_N_Volt");

            double pf = ReadDouble(reader, "Power_Factor");
            double freq = ReadDouble(reader, "Frequency");
            double active = ReadDouble(reader, "Active_Power");
            double reactive = ReadDouble(reader, "Reactive_Power");
            double apparent = ReadDouble(reader, "Apparent_Power");
            double energy = ReadDouble(reader, "Energy");
            double aShift = ReadDouble(reader, "Ashift_Energy");
            double bShift = ReadDouble(reader, "Bshift_Energy");
            double cShift = ReadDouble(reader, "Cshift_Energy");
            double total = ReadDouble(reader, "Total_Energy");

            TotalEnergyText = $"{total:0.##} kWh";
            ActivePowerText = $"{active:0.##} kW";
            TodayEnergyText = $"{energy:0.##} kWh";
            ApparentPowerText = $"{apparent:0.##} kVA";

            L1L2VoltText = $"{l12:0.0} V";
            L2L3VoltText = $"{l23:0.0} V";
            L3L1VoltText = $"{l31:0.0} V";
            L1L2VoltPercent = Math.Min(100, l12 / 450.0 * 100.0);
            L2L3VoltPercent = Math.Min(100, l23 / 450.0 * 100.0);
            L3L1VoltPercent = Math.Min(100, l31 / 450.0 * 100.0);

            L1VoltText = $"{vln1:0.0} V";
            L2VoltText = $"{vln2:0.0} V";
            L3VoltText = $"{vln3:0.0} V";

            L1CurrentText = $"{c1:0.0} A";
            L2CurrentText = $"{c2:0.0} A";
            L3CurrentText = $"{c3:0.0} A";

            double maxV = Math.Max(l12, Math.Max(l23, l31));
            double minV = Math.Min(l12, Math.Min(l23, l31));
            VoltageBalanceStatus = maxV > 0 && ((maxV - minV) / maxV) <= 0.03 ? "Normal" : "Check";

            PowerFactorText = $"{pf:0.00}";
            FrequencyText = $"{freq:0.00} Hz";
            ReactivePowerText = $"{reactive:0.##} kVAr";

            AShiftEnergyText = $"A Shift  {aShift:0.##} kWh";
            BShiftEnergyText = $"B Shift  {bShift:0.##} kWh";
            CShiftEnergyText = $"C Shift  {cShift:0.##} kWh";
            double maxShift = Math.Max(1, Math.Max(aShift, Math.Max(bShift, cShift)));
            AShiftEnergyValue = aShift / maxShift * 100.0;
            BShiftEnergyValue = bShift / maxShift * 100.0;
            CShiftEnergyValue = cShift / maxShift * 100.0;

            WarningText = active > 20 ? "High power usage" : "No active warning";
        }

        private void LoadPowerTrend()
        {
            List<string> labels = new();
            List<double> activePower = new();
            List<double> energy = new();

            using SqlConnection conn = new SqlConnection(modGlobal.connectStr);
            conn.Open();

            string query = @"SELECT TOP 8 LogTime AS TimeValue, Active_Power, Energy
                             FROM EnergyMeterTemp
                             ORDER BY LogTime DESC;
                             SELECT TOP 8 [Hour] AS TimeValue, Active_Power, Energy
                             FROM EnergyMeterLog
                             ORDER BY [Hour] DESC;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                labels.Add(Convert.ToDateTime(reader["TimeValue"]).ToString("HH:mm"));
                activePower.Add(ReadDouble(reader, "Active_Power"));
                energy.Add(ReadDouble(reader, "Energy"));
            }

            if (labels.Count == 0 && reader.NextResult())
            {
                while (reader.Read())
                {
                    labels.Add(Convert.ToDateTime(reader["TimeValue"]).ToString("HH:mm"));
                    activePower.Add(ReadDouble(reader, "Active_Power"));
                    energy.Add(ReadDouble(reader, "Energy"));
                }
            }

            labels.Reverse();
            activePower.Reverse();
            energy.Reverse();

            PowerSeries = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Active Power",
                    Values = activePower,
                    GeometrySize = 8,
                    Stroke = new SolidColorPaint(SKColor.Parse("#2563EB"), 3),
                    Fill = null
                },
                new ColumnSeries<double>
                {
                    Name = "Energy",
                    Values = energy,
                    Fill = new SolidColorPaint(SKColor.Parse("#22C55E"))
                }
            };

            PowerXAxes = new Axis[] { new Axis { Labels = labels } };
            PowerYAxes = new Axis[] { new Axis { MinLimit = 0 } };

            OnPropertyChanged(nameof(PowerSeries));
            OnPropertyChanged(nameof(PowerXAxes));
            OnPropertyChanged(nameof(PowerYAxes));
        }

        private void LoadMonthEnergy()
        {
            DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            DateTime nextMonth = monthStart.AddMonths(1);

            using SqlConnection conn = new SqlConnection(modGlobal.connectStr);
            conn.Open();

            string query = @"SELECT ISNULL(SUM(Energy), 0) AS MonthEnergy
                             FROM EnergyMeterLog
                             WHERE [Hour] >= @MonthStart AND [Hour] < @NextMonth;";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@MonthStart", monthStart);
            cmd.Parameters.AddWithValue("@NextMonth", nextMonth);

            double monthEnergy = Convert.ToDouble(cmd.ExecuteScalar());
            int days = Math.Max(1, DateTime.Today.Day);

            MonthlyEnergyText = $"{monthEnergy:0.##} kWh";
            AverageDayEnergyText = $"{monthEnergy / days:0.##} kWh";
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

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
