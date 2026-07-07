using SkiaSharp;
using System;
using System.Windows;
using System.Windows.Media;

namespace IOTrim
{
    class modGlobal
    {
        private static bool _isPLCConnected;
        public static bool inSimul = true;
        private static string _plcIP = string.Empty;
        private static string _plcPort = string.Empty;
        private static string _opcURL = string.Empty;

        public static bool IsPLCConnected
        {
            get => _isPLCConnected;
            set
            {
                if (_isPLCConnected != value)
                {
                    _isPLCConnected = value;
                    PLCConnectionChanged?.Invoke();
                }
            }
        }

        public static string PlcIP
        {
            get => _plcIP;
            set
            {
                string newValue = value ?? string.Empty;
                if (_plcIP != newValue)
                {
                    _plcIP = newValue;
                    ParametersChanged?.Invoke();
                }
            }
        }

        public static string PlcPort
        {
            get => _plcPort;
            set
            {
                string newValue = value ?? string.Empty;
                if (_plcPort != newValue)
                {
                    _plcPort = newValue;
                    ParametersChanged?.Invoke();
                }
            }
        }

        public static string OPCURL
        {
            get => _opcURL;
            set
            {
                string newValue = value ?? string.Empty;
                if (_opcURL != newValue)
                {
                    _opcURL = newValue;
                    ParametersChanged?.Invoke();
                }
            }
        }

        public static event Action? PLCConnectionChanged;
        public static event Action? ParametersChanged;

        public static readonly string connectStr = @"Data Source=INBLRNB0753;Initial Catalog=IOTrimDB;Integrated Security=True;TrustServerCertificate=True;";


        public static SKColor GetThemeColor(string resourceKey)
        {
            if (Application.Current.Resources[resourceKey] is SolidColorBrush brush)
            {
                return new SKColor(
                    brush.Color.R,
                    brush.Color.G,
                    brush.Color.B,
                    brush.Color.A);
            }

            return SKColors.Black;
        }
    }
}
