using IOTrim.Model;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace IOTrim.Service
{
    internal sealed class OEELogService : IDisposable
    {
        private const int LogIntervalMs = 1000;
        private readonly string connectionString = @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True";
        private readonly string TagOPC = "ns=4;s=|var|AX-564EB0MB1T.Application.Production.";
        private readonly object stateLock = new();
        private CancellationTokenSource? cts;
        private Task? workerTask;
        private bool disposed;
        private int successfulInsertCount;
        private DateTime lastHealthLogTime = DateTime.MinValue;
        private const int LogBeforeHourSeconds = 10;

        public void Start()
        {
            lock (stateLock)
            {
                if (workerTask != null && !workerTask.IsCompleted)
                {
                    LogService.AddLog("Production  service already running.");
                    return;
                }

                cts = new CancellationTokenSource();
                workerTask = Task.Factory.StartNew(
                    () => ProcessQueue(cts.Token),
                    cts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default).Unwrap();
            }

            LogService.AddLog("Production  log service started.");
        }

        public void Stop()
        {
            CancellationTokenSource? localCts;
            Task? localTask;

            lock (stateLock)
            {
                localCts = cts;
                localTask = workerTask;
                cts = null;
                workerTask = null;
            }

            try
            {
                localCts?.Cancel();
                localTask?.Wait(2000);
            }
            catch (AggregateException ex)
            {
                ex.Handle(x => x is OperationCanceledException || x is TaskCanceledException);
            }
            catch (Exception ex)
            {
                LogService.AddException($"Production {LogIntervalMs} ms log service stop failed", ex);
            }
            finally
            {
                localCts?.Dispose();
                LogService.AddLog($"Production {LogIntervalMs} ms log service stopped.");
            }
        }

        private async Task ProcessQueue(CancellationToken token)
        {
            LogService.AddLog("OEE hourly log service started.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    DateTime now = DateTime.Now;

                    DateTime nextHour = new DateTime(
                        now.Year,
                        now.Month,
                        now.Day,
                        now.Hour,
                        0,
                        0).AddHours(1);

                    DateTime logTime = nextHour.AddSeconds(-LogBeforeHourSeconds);

                    if (now >= logTime)
                    {
                        logTime = nextHour.AddHours(1).AddSeconds(-LogBeforeHourSeconds);
                    }

                    TimeSpan waitTime = logTime - now;

                    LogService.AddLog($"Next OEE hourly log scheduled at {logTime:yyyy-MM-dd HH:mm:ss}");

                    await Task.Delay(waitTime, token);

                    if (!AppServices.OpcUaService.IsConnected)
                    {
                        LogService.AddLog("OEE hourly logging skipped. OPC UA is not connected.");
                        continue;
                    }

                    ProductionLogModel data = ReadValuesFromTag();

                    WriteValuesToProductionLog(data);
                    successfulInsertCount++;

                    LogService.AddLog(
                        $"OEE hourly log inserted. InsertCount:{successfulInsertCount}, " +
                        $"ReadTime:{data.LogDateTime:yyyy-MM-dd HH:mm:ss.fff}");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogService.AddException("OEE hourly log service error", ex);
                    await SafeDelay(5000, token);
                }
            }

            LogService.AddLog("OEE hourly log service stopped.");
        }

        private static async Task SafeDelay(int milliseconds, CancellationToken token)
        {
            try
            {
                await Task.Delay(milliseconds, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private ProductionLogModel ReadValuesFromTag()
        {
            return new ProductionLogModel
            {
                LogDateTime = DateTime.Now,
                RunTime = ReadFloatTag($"{TagOPC}Act_Running_Time"),
                ManagementLoss = ReadFloatTag($"{TagOPC}Mgmt_Loss_Time"),
                Idle = ReadFloatTag($"{TagOPC}Idle_Time"),
                PlannedDowntime = ReadFloatTag($"{TagOPC}Planned_Downtime"),
                UnplannedDowntime = ReadFloatTag($"{TagOPC}Unplanned_Downtime"),
                GoodPart = ReadIntTag($"{TagOPC}Good_Parts"),
                BadPart = ReadIntTag($"{TagOPC}Bad_Parts"),
                A = ReadFloatTag($"{TagOPC}Availability"),
                P = ReadFloatTag($"{TagOPC}Performance"),
                Q = ReadFloatTag($"{TagOPC}Quality"),
                OEE = ReadFloatTag($"{TagOPC}OEE")
            };
        }

        private float ReadFloatTag(string nodeId)
        {
            object? value = AppServices.OpcUaService.ReadValue(nodeId);
            if (value == null || value == DBNull.Value)
                return 0f;

            return Convert.ToSingle(value);
        }

        private int ReadIntTag(string nodeId)
        {
            object? value = AppServices.OpcUaService.ReadValue(nodeId);
            if (value == null || value == DBNull.Value)
                return 0;

            return Convert.ToInt32(value);
        }

        private void WriteValuesToProductionLog(ProductionLogModel data)
        {
            using SqlConnection con = new(connectionString);
            using SqlCommand cmd = new(@"
                                    INSERT INTO OEELog
                                    (
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
                                        OEE
                                    )
                                    VALUES
                                    (
                                        @LogDateTime,
                                        @RunTime,
                                        @ManagementLoss,
                                        @Idle,
                                        @PlannedDowntime,
                                        @UnplannedDowntime,
                                        @GoodPart,
                                        @BadPart,
                                        @A,
                                        @P,
                                        @Q,
                                        @OEE
                                    )", con);

            cmd.CommandTimeout = 5;
            cmd.Parameters.Add("@LogDateTime", SqlDbType.DateTime2).Value = data.LogDateTime;
            cmd.Parameters.Add("@RunTime", SqlDbType.Float).Value = data.RunTime;
            cmd.Parameters.Add("@ManagementLoss", SqlDbType.Float).Value = data.ManagementLoss;
            cmd.Parameters.Add("@Idle", SqlDbType.Float).Value = data.Idle;
            cmd.Parameters.Add("@PlannedDowntime", SqlDbType.Float).Value = data.PlannedDowntime;
            cmd.Parameters.Add("@UnplannedDowntime", SqlDbType.Float).Value = data.UnplannedDowntime;
            cmd.Parameters.Add("@GoodPart", SqlDbType.Int).Value = data.GoodPart;
            cmd.Parameters.Add("@BadPart", SqlDbType.Int).Value = data.BadPart;
            cmd.Parameters.Add("@A", SqlDbType.Float).Value = data.A;
            cmd.Parameters.Add("@P", SqlDbType.Float).Value = data.P;
            cmd.Parameters.Add("@Q", SqlDbType.Float).Value = data.Q;
            cmd.Parameters.Add("@OEE", SqlDbType.Float).Value = data.OEE;

            con.Open();
            cmd.ExecuteNonQuery();
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
