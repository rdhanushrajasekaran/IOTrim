using Delta_Riveting;
using IOTrim.Service;
using IOTrim.Views;
using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IOTrim
{
    public partial class MainWindow : Window
    {
        private readonly DataPage dataPage = new DataPage();
        private readonly OneminLogTemp oneminLogTemp = new OneminLogTemp();
        private readonly OEELogService oeeLogService = new OEELogService();
        private ModbusPollingService? modbusPollingService;
        private CancellationTokenSource? opcConnectionCts;
        private Task? opcConnectionTask;
        private readonly object opcConnectionLock = new();
        private const int OpcRetryDelaySeconds = 5;


        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            ReadWriter.Read_From_XML();
            modGlobal.ParametersChanged += ModGlobal_ParametersChanged;
            LogPanel.CloseRequested +=LogPanel_CloseRequested;
            MainFrm.Navigated += MainFrm_Navigated;
            LogService.AddLog("MainWindow constructor completed.");
        }

        private void LogPanel_CloseRequested(object? sender, EventArgs e)
        {
            LogService.AddLog("Log panel close requested.");
            LogPanelContainer.Visibility = Visibility.Collapsed;
        }


        private void MainFrm_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            MainFrm.Opacity = 0;
            MainFrameTransform.Y = 24;

            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(320),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            var slideUp = new DoubleAnimation
            {
                From = 24,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(360),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            MainFrm.BeginAnimation(OpacityProperty, fadeIn);
            MainFrameTransform.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty, slideUp);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.AddLog("Main window loaded.");

                if (!modGlobal.inSimul)
                {

                    modbusPollingService = new ModbusPollingService();
                    modbusPollingService.Start();

                    LogService.AddLog("Main frame navigating to Home page.");
                    StartOpcConnectionInBackground();
                }
                else
                {
                    LogService.AddLog("Simulation mode enabled. Skipping Modbus and OPC UA services.");
                }
                MainFrm.Navigate(new HomePage());
            }
            catch (Exception ex)
            {
                LogService.AddException("Main window load failed", ex);
                MessageBox.Show(
                    "Application loaded, but one or more background services could not start. Please check logs.",
                    "Startup Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void StartOpcConnectionInBackground()
        {
            lock (opcConnectionLock)
            {
                if (opcConnectionTask != null && !opcConnectionTask.IsCompleted)
                {
                    LogService.AddLog("OPC UA background connection already running.");
                    return;
                }

                opcConnectionCts = new CancellationTokenSource();
                CancellationToken token = opcConnectionCts.Token;

                opcConnectionTask = Task.Run(() => OpcConnectionLoopAsync(token), token);
                LogService.AddLog("OPC UA background connection started.");
            }
        }

        private void RestartOpcConnectionInBackground()
        {
            StopOpcConnectionInBackground();
            AppServices.OpcUaService.Disconnect();
            StartOpcConnectionInBackground();
        }

        private void StopOpcConnectionInBackground()
        {
            try
            {
                lock (opcConnectionLock)
                {
                    opcConnectionCts?.Cancel();
                    opcConnectionCts?.Dispose();
                    opcConnectionCts = null;
                    opcConnectionTask = null;
                }

                LogService.AddLog("OPC UA background connection stopped.");
            }
            catch (Exception ex)
            {
                LogService.AddException("OPC UA background connection stop failed", ex);
            }
        }

        private async Task OpcConnectionLoopAsync(CancellationToken token)
        {
            int retryCount = 0;
            while (!token.IsCancellationRequested && retryCount < 3)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(modGlobal.OPCURL))
                    {
                        LogService.AddLog("OPC UA URL is empty. Background connection skipped.");
                        await Task.Delay(TimeSpan.FromSeconds(OpcRetryDelaySeconds), token);
                        continue;
                    }

                    if (!AppServices.OpcUaService.IsConnected)
                    {
                        LogService.AddLog("OPC UA background connection trying to connect.");

                        await AppServices.OpcUaService.ConnectAsync(modGlobal.OPCURL);

                        if (AppServices.OpcUaService.IsConnected)
                        {
                            LogService.AddLog("OPC UA service connected successfully.");
                            retryCount=0; // Reset retry count on successful connection
                            oneminLogTemp.Start();
                            oeeLogService.Start();
                        }
                    }
                    retryCount++;
                    LogService.AddLog($"OPC UA background connection loop completed. Retry count: {retryCount}.");
                    await Task.Delay(TimeSpan.FromSeconds(OpcRetryDelaySeconds), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogService.AddException("OPC UA background connection failed. Retrying silently.", ex);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(OpcRetryDelaySeconds), token);
                        retryCount++;
                        LogService.AddLog($"OPC UA background connection retrying. Retry count: {retryCount}.");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }
        }
        private void Img_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogService.AddLog("Parameter button clicked. Opening AdminLogin.");

                AdminLogin adminLogin = new AdminLogin();
                bool? loginResult = adminLogin.ShowDialog();

                if (loginResult == true && adminLogin.IsAuthenticated)
                {
                    LogService.AddLog("Admin login success. Opening ParameterWindow.");

                    ParameterWindow parameterWindow = new ParameterWindow();
                    bool? parameterResult = parameterWindow.ShowDialog();

                    if (parameterResult == true && parameterWindow.IsSaved)
                    {
                        ApplyParameterChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Parameter window open/apply failed", ex);

                MessageBox.Show(
                    ex.Message,
                    "Parameter Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ModGlobal_ParametersChanged()
        {
            LogService.AddLog($"Runtime parameters changed. PLC:{modGlobal.PlcIP}:{modGlobal.PlcPort}, OPC:{modGlobal.OPCURL}");
        }

        private void ApplyParameterChanges()
        {
            try
            {
                LogService.AddLog("Applying saved parameters without application restart.");

                if (modbusPollingService == null)
                {
                    modbusPollingService = new ModbusPollingService();
                    modbusPollingService.Start();
                }
                else
                {
                    modbusPollingService.Restart();
                }

                LogService.AddLog("OPC UA endpoint changed. Restarting OPC UA background connection.");
                RestartOpcConnectionInBackground();

                LogService.AddLog("Saved parameters applied successfully without closing the application.");
            }
            catch (Exception ex)
            {
                LogService.AddException("Apply parameter changes failed", ex);
                MessageBox.Show("Parameters were saved, but service restart failed:\n\n" + ex.Message, "Parameter Apply Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NavBtnClicked(object sender, RoutedEventArgs e)
        {
            Button ClickedBtn = sender as Button;
            string btnTag = string.Empty;
            
            if ( ClickedBtn != null ) { btnTag = ClickedBtn.Tag as string; }
            
            LogService.AddLog($"Navigation button clicked. Target:{btnTag}");

            try
            {
                switch(btnTag)
                {
                    case "Home":
                        MainFrm.Navigate(new HomePage());
                        LogService.AddLog("Navigated to Home page.");
                        break;

                    case "OEE":
                        MainFrm.Navigate(new OEEPage()); 
                        LogService.AddLog("Navigated to OEE page.");
                        break;

                    case "EMS":
                        MainFrm.Navigate(new EMSPage()); 
                        LogService.AddLog("Navigated to EMS page.");
                        break;

                    case "Data":
                        MainFrm.Navigate(dataPage);
                        LogService.AddLog("Navigated to Data page.");
                        break;

                    case "Logs":
                        LogPanelContainer.Visibility = Visibility.Visible;
                        LogService.AddLog("Log panel opened.");
                        break;

                    default:
                        MainFrm.Navigate(new HomePage());
                        LogService.AddLog("Unknown navigation target. Navigated to Home page.");
                        break;
                }
            }
            catch (Exception ex)
            {
                LogService.AddException($"Navigation failed. Target:{btnTag}", ex);
                MessageBox.Show(ex.Message, "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }


        private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTheme = ThemeService.ToggleTheme();

                if (selectedTheme == AppTheme.Dark)
                {
                    ThemeIconText.Text = "☀";
                    ThemeText.Text = "Light";
                }
                else
                {
                    ThemeIconText.Text = "🌙";
                    ThemeText.Text = "Dark";
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("Theme toggle failed", ex);
                MessageBox.Show(ex.Message, "Theme Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                LogService.AddLog("Main window closing. Stopping services.");
                modGlobal.ParametersChanged -= ModGlobal_ParametersChanged;
                modbusPollingService?.Dispose();
                StopOpcConnectionInBackground();
                oneminLogTemp?.Stop();
                oeeLogService?.Dispose();
                AppServices.OpcUaService.Dispose();
                LogService.AddLog("Main window closed.");
            }
            catch (Exception ex)
            {
                LogService.AddException("Main window close error", ex);
            }

            base.OnClosed(e);
        }
    }
}