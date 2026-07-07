using IOTrim.Model;
using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace IOTrim.Service
{
    public static class ReadWriter
    {
        private static readonly object xmlLock = new object();

        private static readonly string filePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters.xml");

        public static string ParameterFilePath => filePath;

        public static void Write_To_XML(ParameterModel parameterModel)
        {
            try
            {
                if (parameterModel == null)
                    throw new ArgumentNullException(nameof(parameterModel));

                ValidateParameter(parameterModel);

                lock (xmlLock)
                {
                    string? folder = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrWhiteSpace(folder))
                        Directory.CreateDirectory(folder);

                    XDocument doc = new XDocument(
                        new XElement("Parameter",
                            new XElement("PLCIP", parameterModel.PLCIP?.Trim() ?? string.Empty),
                            new XElement("PLCPort", parameterModel.PORT.ToString(CultureInfo.InvariantCulture)),
                            new XElement("OPCURL", parameterModel.OPCURL?.Trim() ?? string.Empty)
                        )
                    );

                    doc.Save(filePath);
                }

                Apply_To_Global(parameterModel);
                LogService.AddLog($"Parameters saved and applied. PLC:{modGlobal.PlcIP}:{modGlobal.PlcPort}, OPC:{modGlobal.OPCURL}");
            }
            catch (Exception ex)
            {
                LogService.AddException("Parameter XML write failed", ex);
                throw;
            }
        }

        public static ParameterModel Read_From_XML()
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    LogService.AddLog($"Parameters.xml file not found. Creating default file. Path:{filePath}");

                    ParameterModel defaultModel = GetDefaultParameter();
                    Write_To_XML(defaultModel);
                    return defaultModel;
                }

                XDocument doc;

                lock (xmlLock)
                {
                    doc = XDocument.Load(filePath);
                }

                XElement? root = doc.Element("Parameter");

                if (root == null)
                    throw new InvalidDataException("Invalid Parameters.xml format. Root node <Parameter> not found.");

                string plcIP = (root.Element("PLCIP")?.Value ?? string.Empty).Trim();
                string plcPortText = (root.Element("PLCPort")?.Value ?? string.Empty).Trim();
                string opcURL = (root.Element("OPCURL")?.Value ?? string.Empty).Trim();

                if (!int.TryParse(plcPortText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int plcPort))
                    throw new InvalidDataException($"Invalid PLC port value in Parameters.xml: {plcPortText}");

                ParameterModel parameterModel = new ParameterModel
                {
                    PLCIP = plcIP,
                    PORT = plcPort,
                    OPCURL = opcURL
                };

                ValidateParameter(parameterModel);
                Apply_To_Global(parameterModel);

                LogService.AddLog($"Parameters loaded and applied. PLC:{modGlobal.PlcIP}:{modGlobal.PlcPort}, OPC:{modGlobal.OPCURL}");
                return parameterModel;
            }
            catch (Exception ex)
            {
                LogService.AddException("Parameter XML read failed. Default parameters will be applied", ex);

                ParameterModel defaultModel = GetDefaultParameter();
                Apply_To_Global(defaultModel);
                return defaultModel;
            }
        }

        public static void Apply_To_Global(ParameterModel parameterModel)
        {
            ValidateParameter(parameterModel);

            modGlobal.PlcIP = parameterModel.PLCIP?.Trim() ?? string.Empty;
            modGlobal.PlcPort = parameterModel.PORT.ToString(CultureInfo.InvariantCulture);
            modGlobal.OPCURL = parameterModel.OPCURL?.Trim() ?? string.Empty;
        }

        private static void ValidateParameter(ParameterModel parameterModel)
        {
            if (string.IsNullOrWhiteSpace(parameterModel.PLCIP))
                throw new InvalidDataException("PLC IP address cannot be empty.");

            if (parameterModel.PORT <= 0 || parameterModel.PORT > 65535)
                throw new InvalidDataException("PLC port must be between 1 and 65535.");

            if (string.IsNullOrWhiteSpace(parameterModel.OPCURL))
                throw new InvalidDataException("OPC UA URL cannot be empty.");
        }

        private static ParameterModel GetDefaultParameter()
        {
            return new ParameterModel
            {
                PLCIP = "192.168.1.5",
                PORT = 502,
                OPCURL = "opc.tcp://10.67.107.242:4841/freeopcua/server/"
            };
        }
    }
}
