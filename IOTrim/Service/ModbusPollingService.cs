using EasyModbus;
using IOTrim.Model;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace IOTrim.Service
{
    public sealed class ModbusPollingService : IDisposable
    {
        private ModbusClient? modbusClient;

        private string ipAddress = string.Empty;
        private int port = 502;
        private readonly object modbusLock = new object();

        private DataModel? dataModel;
        private CancellationTokenSource? cancellationTokenSource;
        private Task? pollingTask;
        private volatile bool isStarted;
        private bool disposed;

        private readonly string connectStr = @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True;";

        private bool IsConnected => modbusClient != null && modbusClient.Connected;

        private const int PollingIntervalMs = 150;
        private const int ReconnectDelayMs = 3000;
        private const int MaxRetryCount = 3;

        #region PLC Addresses

        private const int HeartBeatAddress = 22400;

        private const int LogTriggerAddress = 22403;
        private const int LogTriggerAckAddress = 22405;
        private const int LogResultAddress = 22407;

        private const int BlSerialNumberAddress = 20000;
        private const int HolderSerialNumberAddress = 20020;
        private const int ScrewingTorqueAddress = 20030;
        private const int ScrewingAngleAddress = 20070;
        private const int BlPressingLoadAddress = 20040;
        private const int ProfiloMeterResultAddress = 205;
        private const int ScrewingResultAddress = 200;
        private const int VariantAddress = 20080;
        private const int ColorAddress = 20090;
        private const int ProfiloMeterHeightAddress = 20100;
        private const int CountAddress = 20110;
        private const int ResultAddress = 20120;
        private const int ShiftAddress = 20130;
        private const int FixtureNumberAddress = 20140;
        private const int StationNameAddress = 20150;

        #endregion

        private const int TriggerValue = 101;
        private const int SuccessValue = 200;
        private const int FailedValue = 400;
        private const int ErrorValue = 500;

        public void Start()
        {
            try
            {
                if (isStarted)
                {
                    LogService.AddLog("Modbus polling service already running.");
                    return;
                }

                LoadConnectionParameters();
                LogService.AddLog($"Modbus polling service starting. IP:{ipAddress}, Port:{port}");
                isStarted = true;
                cancellationTokenSource = new CancellationTokenSource();

                pollingTask = Task.Factory.StartNew(
                    () => PollingLoop(cancellationTokenSource.Token),
                    cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                
                LogService.AddLog("Modbus polling service started.");
            }
            catch (Exception ex)
            {
                isStarted = false;
                LogService.AddException("Modbus polling service start failed", ex);
                throw;
            }
        }

        


        private void LoadConnectionParameters()
        {
            try
            {
                ipAddress = modGlobal.PlcIP?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(ipAddress))
                    throw new InvalidOperationException("PLC IP address is empty. Please update Parameters.xml.");

                if (!int.TryParse(modGlobal.PlcPort, out port) || port <= 0 || port > 65535)
                    throw new InvalidOperationException($"Invalid PLC port: {modGlobal.PlcPort}. Please update Parameters.xml.");
            }
            catch (Exception ex)
            {
                LogService.AddException("Loading Modbus connection parameters failed", ex);
                throw;
            }
        }

        public void Restart()
        {
            try
            {
                LogService.AddLog("Modbus polling service restart requested after parameter change.");
                Stop();
                Start();
            }
            catch (Exception ex)
            {
                LogService.AddException("Modbus polling service restart failed", ex);
                throw;
            }
        }

        private void PollingLoop(CancellationToken token)
        {
            LogService.AddLog("Modbus polling loop entered.");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (!EnsureConnected(token))
                    {
                        SafeDelay(ReconnectDelayMs, token);
                        continue;
                    }

                    WriteRegister(HeartBeatAddress, 100); // HeartBeat Trigger

                    int trigger = ReadIntRegister(LogTriggerAddress);
                    int ack = ReadIntRegister(LogTriggerAckAddress);

                    if (trigger == TriggerValue && ack == 0)
                    {
                        WriteRegister(LogTriggerAckAddress, SuccessValue);
                        LogService.AddLog($"PLC log trigger received. Trigger:{trigger}, Ack:{ack}");
                        ProcessLogRequest();
                    }

                }
                catch (Exception ex)
                {
                    LogService.AddException("Modbus polling loop error", ex);
                    TryDisconnect();
                    SafeDelay(ReconnectDelayMs, token);
                }

                SafeDelay(PollingIntervalMs, token);
            }
        }

        private bool EnsureConnected(CancellationToken token)
        {
            if (IsConnected)
                return true;

            for (int attempt = 1; attempt <= MaxRetryCount && !token.IsCancellationRequested; attempt++)
            {
                try
                {
                    TryDisconnect();

                    if (string.IsNullOrWhiteSpace(ipAddress) || port <= 0)
                        LoadConnectionParameters();

                    modbusClient = new ModbusClient(ipAddress, port);
                    modbusClient.Connect();

                    if (IsConnected)
                    {
                        modGlobal.IsPLCConnected = IsConnected;
                        LogService.AddLog($"Modbus connected successfully. IP:{ipAddress}, Port:{port}, Attempt:{attempt}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    modGlobal.IsPLCConnected = IsConnected;
                    LogService.AddException($"Modbus connect failed. Attempt {attempt}/{MaxRetryCount}", ex);
                }

                SafeDelay(1000, token);
            }

            LogService.AddLog($"Modbus connection failed after {MaxRetryCount} attempts.");
            return false;
        }

        private void ProcessLogRequest()
        {
            try
            {
                LogService.AddLog("Processing PLC log request started.");

                if (!WriteRegister(LogTriggerAckAddress, SuccessValue))
                {
                    LogService.AddLog("PLC log request failed. Unable to write trigger ACK.");
                    WriteRegister(LogResultAddress, FailedValue);
                    return;
                }

                LogService.AddLog("PLC log trigger ACK written successfully.");

                string variant = ReadStringFromRegisters(VariantAddress, 5);

                dataModel = new DataModel
                {
                    BLSerialNo = ReadStringFromRegisters(BlSerialNumberAddress, 20),
                    HolderSerialNo = ReadStringFromRegisters(HolderSerialNumberAddress, 20),
                    FixtureNumber = ReadStringFromRegisters(FixtureNumberAddress, 10),
                    StationName = "IO Trim",
                    Shift = ShiftCal(),
                    ScrewingTorque = ReadFloatRegister(ScrewingTorqueAddress),
                    ScrewingAngle = ReadIntRegister(ScrewingAngleAddress),
                    ScrewingResult = ReadBoolRegister(ScrewingResultAddress),
                    BLPressingLoad = ReadIntRegister(BlPressingLoadAddress),
                    ProfiloMeterResult = ReadBoolRegister(ProfiloMeterResultAddress),
                    ProfiloMeterHeight = ReadFloatRegister(ProfiloMeterHeightAddress),
                    Variant = variant,
                    Color = variant == "PVD" ? "Black" : "Brown",
                    Count = ReadIntRegister(CountAddress),
                    Result = ReadStringFromRegisters(ResultAddress, 10)
                };

                LogService.AddLog($"PLC data read completed. BL:{dataModel.BLSerialNo}, Holder:{dataModel.HolderSerialNo}, Result:{dataModel.Result}");

                bool insertStatus = InsertProductionLog(dataModel);

                if (insertStatus)
                {
                    WriteRegister(LogResultAddress, SuccessValue);
                    LogService.AddLog("Production log inserted successfully. PLC result set to SUCCESS.");
                }
                else
                {
                    WriteRegister(LogResultAddress, ErrorValue);
                    LogService.AddLog("Production log insert failed. PLC result set to ERROR.");
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("PLC log process error", ex);
                WriteRegister(LogResultAddress, ErrorValue);
            }
        }

        private string ShiftCal()
        {
            TimeSpan current = DateTime.Now.TimeOfDay;

            if (current >= new TimeSpan(6, 0, 0) &&
                current < new TimeSpan(14, 0, 0))
            {
                return "A";
            }

            if (current >= new TimeSpan(14, 0, 0) &&
                current < new TimeSpan(22, 0, 0))
            {
                return "B";
            }

            return "C";
        }

        private bool InsertProductionLog(DataModel model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectStr))
                {
                    conn.Open();

                    if(conn.State == System.Data.ConnectionState.Open)
                    {
                        LogService.AddLog("Database connection opened for PLC production log insert.");
                        string query = @"
                                        INSERT INTO ProductionLog
                                        (
                                            DateTime,
                                            BLSerialNo,
                                            HolderSerialNo,
                                            FixtureNumber,
                                            StationName,
                                            Shift,
                                            ScrewingTorque,
                                            ScrewingAngle,
                                            ScrewingResult,
                                            BLPressingLoad,
                                            ProfiloMeterResult,
                                            ProfiloMeterHeight,
                                            Variant,
                                            Color,
                                            [Count],
                                            Result
                                        )
                                        VALUES
                                        (
                                            @DateTime,
                                            @BLSerialNo,
                                            @HolderSerialNo,
                                            @FixtureNumber,
                                            @StationName,
                                            @Shift,
                                            @ScrewingTorque,
                                            @ScrewingAngle,
                                            @ScrewingResult,
                                            @BLPressingLoad,
                                            @ProfiloMeterResult,
                                            @ProfiloMeterHeight,
                                            @Variant,
                                            @Color,
                                            @Count,
                                            @Result
                                        )";

                        using(SqlCommand cmd = new SqlCommand(query,conn))
                        {
                            cmd.Parameters.AddWithValue("@DateTime", DateTime.Now);
                            cmd.Parameters.AddWithValue("@BLSerialNo", model.BLSerialNo ?? "");
                            cmd.Parameters.AddWithValue("@HolderSerialNo", model.HolderSerialNo ?? "");
                            cmd.Parameters.AddWithValue("@FixtureNumber", model.FixtureNumber ?? "");
                            cmd.Parameters.AddWithValue("@StationName", model.StationName ?? "");
                            cmd.Parameters.AddWithValue("@Shift", model.Shift ?? "");

                            cmd.Parameters.AddWithValue("@ScrewingTorque", model.ScrewingTorque);
                            cmd.Parameters.AddWithValue("@ScrewingAngle", model.ScrewingAngle);
                            cmd.Parameters.AddWithValue("@ScrewingResult", model.ScrewingResult);
                            cmd.Parameters.AddWithValue("@BLPressingLoad", model.BLPressingLoad);
                            cmd.Parameters.AddWithValue("@ProfiloMeterResult", model.ProfiloMeterResult);
                            cmd.Parameters.AddWithValue("@ProfiloMeterHeight", model.ProfiloMeterHeight);

                            cmd.Parameters.AddWithValue("@Variant", model.Variant ?? "");
                            cmd.Parameters.AddWithValue("@Color", model.Color ?? "");
                            cmd.Parameters.AddWithValue("@Count", model.Count);
                            cmd.Parameters.AddWithValue("@Result", model.Result ?? "");

                            int rowsAffected = cmd.ExecuteNonQuery();
                            LogService.AddLog($"ProductionLog insert executed. Rows affected:{rowsAffected}, BL:{model.BLSerialNo}, Result:{model.Result}");
                        }
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("ProductionLog insert failed", ex);
                return false;
            }
        }

        public int ReadIntRegister(int address)
        {
            try
            {
                if (!IsConnected)
                    return -1;

                lock (modbusLock)
                {
                    return modbusClient!.ReadHoldingRegisters(address, 1)[0];
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Read integer register failed. Address:{address}", ex);
                TryDisconnect();
                return -1;
            }
        }

        public float ReadFloatRegister(int address)
        {
            try
            {
                if (!IsConnected)
                    return -1f;

                lock (modbusLock)
                {
                    int[] registers = modbusClient!.ReadHoldingRegisters(address, 2);

                    uint value = (uint)(registers[1] << 16 | registers[0]);

                    return BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Read float register failed. Address:{address}", ex);
                TryDisconnect();
                return -1f;
            }
        }

       
        public bool ReadBoolRegister(int address)
        {
            try
            {
                if (!IsConnected)
                    return false;

                lock (modbusLock)
                {
                    bool value = modbusClient!.ReadCoils(address, 1)[0];
                    return value;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Read bool register failed. Address:{address}", ex);
                TryDisconnect();
                return false;
            }
        }

        public string ReadStringFromRegisters(int startAddress, int length)
        {
            try
            {
                if (!IsConnected)
                    return "";

                lock (modbusLock)
                {
                    int registersNeeded = (length + 1) / 2;
                    int[] registers = modbusClient!.ReadHoldingRegisters(startAddress, registersNeeded);

                    char[] chars = new char[length];
                    for (int i = 0; i < length; i++)
                    {
                        int registerIndex = i / 2;
                        int byteIndex = i % 2;

                        if (byteIndex == 0)
                            chars[i] = (char)(registers[registerIndex] & 0xFF);
                        else
                            chars[i] = (char)((registers[registerIndex] >> 8) & 0xFF);
                    }

                    return new string(chars).TrimEnd('\0');
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Read string register failed. Address:{startAddress}, Length:{length}", ex);
                TryDisconnect();
                return "";
            }
        }

        public bool WriteRegister(int address, int value)
        {
            try
            {
                if (!IsConnected)
                    return false;

                lock (modbusLock)
                {
                    modbusClient!.WriteSingleRegister(address, value);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Write register failed. Address:{address}, Value:{value}", ex);
                TryDisconnect();
                return false;
            }
        }

        private void SafeDelay(int milliseconds, CancellationToken token)
        {
            try
            {
                Task.Delay(milliseconds, token).Wait(token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                LogService.AddException($"Safe delay failed. DelayMs:{milliseconds}", ex);
            }
        }

        private void TryDisconnect()
        {
            try
            {
                lock (modbusLock)
                {
                    if (modbusClient != null)
                    {
                        if (modbusClient.Connected)
                        {
                            modbusClient.Disconnect();
                            LogService.AddLog("Modbus disconnected.");
                        }

                        modbusClient = null;
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Modbus disconnect failed", ex);
                modbusClient = null;
            }
        }

        public void Stop()
        {
            try
            {
                if (!isStarted)
                    return;

                LogService.AddLog("Modbus polling service stopping.");
                cancellationTokenSource?.Cancel();
                try
                {
                    pollingTask?.Wait(2000);
                }
                catch (AggregateException ex)
                {
                    ex.Handle(x => x is OperationCanceledException || x is TaskCanceledException);
                }
                TryDisconnect();
            }
            catch (Exception ex)
            {
                LogService.AddException("Modbus service stop failed", ex);
            }
            finally
            {
                isStarted = false;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                pollingTask = null;
                LogService.AddLog("Modbus polling service stopped.");
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