using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace IOTrim.Service
{
    public static class LogService
    {
        public static event Action<string>? LogReceived;

        private static readonly AutoResetEvent queueSignal = new(false);
        private static readonly ConcurrentQueue<string> logQueue = new();
        private static readonly object startStopLock = new();
        private static readonly object fileLock = new();

        private static CancellationTokenSource? cancellationTokenSource;
        private static Task? workerTask;
        private static bool isStarted;

        public static string BaseFolderPath { get; set; } = @"D:\Logs\";

        private static string CurrentFolderPath =>
            Path.Combine(BaseFolderPath, $"Year - {DateTime.Now:yyyy}", $"Month - {DateTime.Now:MM}");

        public static void Start()
        {
            lock (startStopLock)
            {
                if (isStarted)
                    return;

                cancellationTokenSource = new CancellationTokenSource();
                workerTask = Task.Factory.StartNew(
                    () => ProcessQueue(cancellationTokenSource.Token),
                    cancellationTokenSource.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                isStarted = true;
            }

            AddLog("Log service started.");
        }

        public static void AddLog(string message)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(message))
                    return;

                string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] => {message}";
                logQueue.Enqueue(log);
                queueSignal.Set();
                RaiseLogReceived(log);
            }
            catch
            {
                // Logging must never crash the application.
            }
        }

        public static void AddException(string operationName, Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                AddLog($"CANCELLED | {operationName}");
                return;
            }

            AddLog($"EXCEPTION | {operationName} | {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex}");
        }

        private static void RaiseLogReceived(string log)
        {
            try
            {
                Dispatcher? dispatcher = Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished)
                    return;

                if (dispatcher.CheckAccess())
                    LogReceived?.Invoke(log);
                else
                    dispatcher.BeginInvoke(new Action(() => LogReceived?.Invoke(log)), DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                WriteInternalError("Log UI event failed", ex);
            }
        }

        private static void ProcessQueue(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    queueSignal.WaitOne(1000);
                    FlushQueue();
                }
                catch (Exception ex)
                {
                    WriteInternalError("Log queue processing failed", ex);
                }
            }

            FlushQueue();
        }

        private static void FlushQueue()
        {
            int written = 0;
            while (written < 1000 && logQueue.TryDequeue(out string? log))
            {
                WriteToFile(log);
                written++;
            }

            if (!logQueue.IsEmpty)
                queueSignal.Set();
        }

        private static void WriteToFile(string log)
        {
            try
            {
                Directory.CreateDirectory(CurrentFolderPath);
                string filePath = Path.Combine(CurrentFolderPath, $"IOTrim_Log_{DateTime.Now:yyyyMMdd}.txt");

                lock (fileLock)
                {
                    File.AppendAllText(filePath, log + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                WriteInternalError("Write log file failed", ex);
            }
        }

        private static void WriteInternalError(string title, Exception ex)
        {
            try
            {
                Directory.CreateDirectory(CurrentFolderPath);
                string filePath = Path.Combine(CurrentFolderPath, $"IOTrim_Error_{DateTime.Now:yyyyMMdd}.txt");
                lock (fileLock)
                {
                    File.AppendAllText(filePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {title}{Environment.NewLine}{ex}{Environment.NewLine}{Environment.NewLine}", Encoding.UTF8);
                }
            }
            catch
            {
            }
        }

        public static void Stop()
        {
            lock (startStopLock)
            {
                if (!isStarted)
                    return;

                try
                {
                    AddLog("Log service stopping.");
                    cancellationTokenSource?.Cancel();
                    queueSignal.Set();
                    workerTask?.Wait(2000);
                    FlushQueue();
                }
                catch (AggregateException ex)
                {
                    ex.Handle(x => x is OperationCanceledException || x is TaskCanceledException);
                }
                catch (Exception ex)
                {
                    WriteInternalError("Log service stop failed", ex);
                }
                finally
                {
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = null;
                    workerTask = null;
                    isStarted = false;
                }
            }
        }
    }
}
