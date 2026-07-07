using IOTrim.Model;
using IOTrim.Service;
using System;
using System.Globalization;
using System.Windows;

namespace IOTrim
{
    public partial class ParameterWindow : Window
    {
        public bool IsSaved { get; private set; }

        public ParameterWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                ParameterModel parameterModel = ReadWriter.Read_From_XML();

                txtPlcIp.Text = parameterModel.PLCIP;
                txtPort.Text = parameterModel.PORT.ToString(CultureInfo.InvariantCulture);
                txtOpcUrl.Text = parameterModel.OPCURL;

                LogService.AddLog("Parameter window loaded with current XML values.");
            }
            catch (Exception ex)
            {
                LogService.AddException("Parameter window load failed", ex);
                MessageBox.Show(
                    "Unable to load Parameters.xml. Please check the file format.\n\n" + ex.Message,
                    "Parameter Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Save_Para_Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string plcIP = txtPlcIp.Text.Trim();
                string plcPortText = txtPort.Text.Trim();
                string opcURL = txtOpcUrl.Text.Trim();

                if (string.IsNullOrWhiteSpace(plcIP))
                {
                    MessageBox.Show("Please enter PLC IP address.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPlcIp.Focus();
                    return;
                }

                if (!int.TryParse(plcPortText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int plcPort) || plcPort <= 0 || plcPort > 65535)
                {
                    MessageBox.Show("Please enter a valid PLC port between 1 and 65535.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPort.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(opcURL))
                {
                    MessageBox.Show("Please enter OPC UA URL.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtOpcUrl.Focus();
                    return;
                }

                ParameterModel parameterModel = new ParameterModel
                {
                    PLCIP = plcIP,
                    PORT = plcPort,
                    OPCURL = opcURL
                };

                ReadWriter.Write_To_XML(parameterModel);

                IsSaved = true;

                MessageBox.Show("Parameters saved and applied successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            catch (Exception ex)
            {
                LogService.AddException("Parameter save failed", ex);
                MessageBox.Show("Parameter save failed:\n\n" + ex.Message, "Parameter Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
