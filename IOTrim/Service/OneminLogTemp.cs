using IOTrim.Model;
using Opc.Ua;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace IOTrim.Service
{
    internal sealed class OneminLogTemp : IDisposable
    {
        private CancellationTokenSource? cts;
        private Task? workerTask;
        private bool disposed;
        private int logInterval = 1; // Interval in minutes for logging data
        private readonly string connectionString = @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True";
        private string TagOPC = "ns=4;s=|var|AX-564EB0MB1T.Application.EMS.";

        public void Start()
        {
            try
            {
                if (workerTask != null && !workerTask.IsCompleted)
                {
                    LogService.AddLog($"EMS {logInterval}-minute temp log service already running.");
                    return;
                }

                cts = new CancellationTokenSource();

                workerTask = Task.Factory.StartNew(
                    ProcessQueue,
                    cts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();

                LogService.AddLog($"EMS {logInterval}-minute temp log service started.");
            }
            catch (Exception ex)
            {
                LogService.AddException($"EMS {logInterval}-minute temp log service start failed", ex);
            }
        }

        public void Stop()
        {
            try
            {
                LogService.AddLog($"EMS {logInterval}-minute temp log service stopping.");
                cts?.Cancel();
                try
                {
                    workerTask?.Wait(2000);
                }
                catch (AggregateException ex)
                {
                    ex.Handle(x => x is OperationCanceledException || x is TaskCanceledException);
                }
                LogService.AddLog($"EMS {logInterval}-minute temp log service stopped.");
            }
            catch (Exception ex)
            {
                LogService.AddException($"EMS {logInterval}-minute temp log service stop failed", ex);
            }
            finally
            {
                cts?.Dispose();
                cts = null;
                workerTask = null;
            }
        }

        private async Task ProcessQueue()
        {
            LogService.AddLog($"EMS {logInterval}-minute temp loop entered.");

            while (cts != null && !cts.Token.IsCancellationRequested)
            {
                try
                {
                    EMSModel data = ReadValuesFromTag();

                    if (data != null)
                    {
                        WriteValuesToTempTable(data);
                        LogService.AddLog($"EMS temp values inserted. LogTime:{data.LogTime:yyyy-MM-dd HH:mm:ss}, TotalEnergy:{data.Total_Energy}");
                    }

                    int tempCount = GetTempTableCount();

                    if (tempCount >= 60)
                    {
                        LogService.AddLog($"EMS summary started. Temp count:{tempCount}");

                        InsertLogFromTemp();
                        DeleteAllTempData();

                        LogService.AddLog("EMS summary completed and temp table cleared.");
                    }
                }
                catch (OperationCanceledException)
                {
                    LogService.AddLog($"EMS {logInterval}-minute temp loop cancellation requested.");
                    break;
                }
                catch (Exception ex)
                {
                    LogService.AddException($"EMS {logInterval}-minute temp loop error", ex);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(logInterval), cts.Token);
                }
                catch (OperationCanceledException)
                {
                    LogService.AddLog("EMS one-minute temp loop delay cancelled.");
                    break;
                }
            }

            LogService.AddLog($"EMS {logInterval}-minute temp loop exited.");
        }
        private int GetTempTableCount()
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);

                string query = "SELECT COUNT(*) FROM EnergyMeterTemp";

                using SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS temp count read failed", ex);
                throw;
            }
        }
        private void DeleteAllTempData()
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);

                string query = "DELETE FROM EnergyMeterTemp";

                using SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                LogService.AddLog($"EMS temp table cleared. Rows:{rowsAffected}");
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS temp clear failed", ex);
                throw;
            }
        }

        private EMSModel ReadValuesFromTag()
        {
            try
            {
                LogService.AddLog("EMS OPC UA read started.");

                EMSModel model = new EMSModel
                {
                    LogTime = DateTime.Now,

                    L1_L2_Volt = ReadFloatTag($"{TagOPC}L1_L2_Volt"),
                    L2_L3_Volt = ReadFloatTag($"{TagOPC}L2_L3_Volt"),
                    L3_L1_Volt = ReadFloatTag($"{TagOPC}L3_L1_Volt"),

                    L1_N_Volt = ReadFloatTag($"{TagOPC}L1_N_Volt"),
                    L2_N_Volt = ReadFloatTag($"{TagOPC}L2_N_Volt"),
                    L3_N_Volt = ReadFloatTag($"{TagOPC}L3_N_Volt"),

                    L1_C = ReadFloatTag($"{TagOPC}L1_C"),
                    L2_C = ReadFloatTag($"{TagOPC}L2_C"),
                    L3_C = ReadFloatTag($"{TagOPC}L3_C"),

                    Power_Factor = ReadFloatTag($"{TagOPC}Power_Factor"),
                    Frequency = ReadFloatTag($"{TagOPC}Frequency"),

                    Active_Power = ReadFloatTag($"{TagOPC}Active_Power"),
                    Reactive_Power = ReadFloatTag($"{TagOPC}Reactive_Power"),
                    Apparent_Power = ReadFloatTag($"{TagOPC}Apparent_Power"),

                    Energy = ReadFloatTag($"{TagOPC}Energy"),
                    Ashift_Energy = ReadFloatTag($"{TagOPC}Ashift_Energy"),
                    Bshift_Energy = ReadFloatTag($"{TagOPC}Bshift_Energy"),
                    Cshift_Energy = ReadFloatTag($"{TagOPC}Cshift_Energy"),
                    Total_Energy = ReadFloatTag($"{TagOPC}Total_Energy")
                };

                LogService.AddLog("EMS OPC UA read completed.");
                return model;
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS OPC UA read failed", ex);
                throw;
            }
        }

        private float ReadFloatTag(string nodeId)
        {
            try
            {
                object? value = AppServices.OpcUaService.ReadValue(nodeId);
                return Convert.ToSingle(value);
            }
            catch (Exception ex)
            {
                LogService.AddException($"EMS OPC UA tag read failed. NodeId:{nodeId}", ex);
                throw;
            }
        }

        private void WriteValuesToTempTable(EMSModel data)
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);

                string query = @"
            INSERT INTO EnergyMeterTemp
            (
                LogTime,
                L1_L2_Volt, L2_L3_Volt, L3_L1_Volt,
                L1_N_Volt, L2_N_Volt, L3_N_Volt,
                L1_C, L2_C, L3_C,
                Power_Factor, Frequency,
                Active_Power, Reactive_Power, Apparent_Power,
                Energy, Ashift_Energy, Bshift_Energy, Cshift_Energy, Total_Energy
            )
            VALUES
            (
                @LogTime,
                @L1_L2_Volt, @L2_L3_Volt, @L3_L1_Volt,
                @L1_N_Volt, @L2_N_Volt, @L3_N_Volt,
                @L1_C, @L2_C, @L3_C,
                @Power_Factor, @Frequency,
                @Active_Power, @Reactive_Power, @Apparent_Power,
                @Energy, @Ashift_Energy, @Bshift_Energy, @Cshift_Energy, @Total_Energy
            )";

                using SqlCommand cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@LogTime", data.LogTime);

                cmd.Parameters.AddWithValue("@L1_L2_Volt", data.L1_L2_Volt);
                cmd.Parameters.AddWithValue("@L2_L3_Volt", data.L2_L3_Volt);
                cmd.Parameters.AddWithValue("@L3_L1_Volt", data.L3_L1_Volt);

                cmd.Parameters.AddWithValue("@L1_N_Volt", data.L1_N_Volt);
                cmd.Parameters.AddWithValue("@L2_N_Volt", data.L2_N_Volt);
                cmd.Parameters.AddWithValue("@L3_N_Volt", data.L3_N_Volt);

                cmd.Parameters.AddWithValue("@L1_C", data.L1_C);
                cmd.Parameters.AddWithValue("@L2_C", data.L2_C);
                cmd.Parameters.AddWithValue("@L3_C", data.L3_C);

                cmd.Parameters.AddWithValue("@Power_Factor", data.Power_Factor);
                cmd.Parameters.AddWithValue("@Frequency", data.Frequency);

                cmd.Parameters.AddWithValue("@Active_Power", data.Active_Power);
                cmd.Parameters.AddWithValue("@Reactive_Power", data.Reactive_Power);
                cmd.Parameters.AddWithValue("@Apparent_Power", data.Apparent_Power);

                cmd.Parameters.AddWithValue("@Energy", data.Energy);
                cmd.Parameters.AddWithValue("@Ashift_Energy", data.Ashift_Energy);
                cmd.Parameters.AddWithValue("@Bshift_Energy", data.Bshift_Energy);
                cmd.Parameters.AddWithValue("@Cshift_Energy", data.Cshift_Energy);
                cmd.Parameters.AddWithValue("@Total_Energy", data.Total_Energy);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                LogService.AddLog($"EMS temp insert completed. Rows:{rowsAffected}, LogTime:{data.LogTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS temp insert failed", ex);
                throw;
            }
        }

        public void InsertLogFromTemp()
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);

                string query = @"
            INSERT INTO EnergyMeterLog
            (
                [Hour],
                L1_L2_Volt, L2_L3_Volt, L3_L1_Volt,
                L1_N_Volt, L2_N_Volt, L3_N_Volt,
                L1_C, L2_C, L3_C,
                Power_Factor, Frequency,
                Active_Power, Reactive_Power, Apparent_Power,
                Energy, Ashift_Energy, Bshift_Energy, Cshift_Energy, Total_Energy
            )
            SELECT
                MIN(LogTime),
                AVG(L1_L2_Volt), AVG(L2_L3_Volt), AVG(L3_L1_Volt),
                AVG(L1_N_Volt), AVG(L2_N_Volt), AVG(L3_N_Volt),
                AVG(L1_C), AVG(L2_C), AVG(L3_C),
                AVG(Power_Factor), AVG(Frequency),
                AVG(Active_Power), AVG(Reactive_Power), AVG(Apparent_Power),
                SUM(Energy), SUM(Ashift_Energy), SUM(Bshift_Energy),
                SUM(Cshift_Energy), SUM(Total_Energy)
            FROM EnergyMeterTemp;";

                using SqlCommand cmd = new SqlCommand(query, con);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                LogService.AddLog($"EMS log insert completed from temp table. Rows:{rowsAffected}");
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS log insert from temp failed", ex);
                throw;
            }
        }

        public void InsertHourlyLogFromTemp(DateTime fromTime, DateTime toTime)
        {
            try
            {
                using SqlConnection con = new SqlConnection(connectionString);

                string query = @"INSERT INTO EnergyMeterLog
                    (
                        [Hour],
                        L1_L2_Volt, L2_L3_Volt, L3_L1_Volt,
                        L1_N_Volt, L2_N_Volt, L3_N_Volt,
                        L1_C, L2_C, L3_C,
                        Power_Factor, Frequency,
                        Active_Power, Reactive_Power, Apparent_Power,
                        Energy, Ashift_Energy, Bshift_Energy, Cshift_Energy, Total_Energy
                    )
                    SELECT
                        DATEADD(HOUR, DATEDIFF(HOUR, 0, LogTime), 0),
                        AVG(L1_L2_Volt), AVG(L2_L3_Volt), AVG(L3_L1_Volt),
                        AVG(L1_N_Volt), AVG(L2_N_Volt), AVG(L3_N_Volt),
                        AVG(L1_C), AVG(L2_C), AVG(L3_C),
                        AVG(Power_Factor), AVG(Frequency),
                        AVG(Active_Power), AVG(Reactive_Power), AVG(Apparent_Power),
                        SUM(Energy), SUM(Ashift_Energy), SUM(Bshift_Energy),
                        SUM(Cshift_Energy), SUM(Total_Energy)
                    FROM EnergyMeterTemp
                    WHERE LogTime >= @FromTime
                      AND LogTime < @ToTime
                    GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, LogTime), 0);";

                using SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@FromTime", fromTime);
                cmd.Parameters.AddWithValue("@ToTime", toTime);

                con.Open();
                int rowsAffected = cmd.ExecuteNonQuery();
                LogService.AddLog($"EMS hourly insert completed. Rows:{rowsAffected}, From:{fromTime:yyyy-MM-dd HH:mm:ss}, To:{toTime:yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                LogService.AddException("EMS hourly insert failed", ex);
                throw;
            }
        }
        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
