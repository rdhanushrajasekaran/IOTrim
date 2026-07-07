using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOTrim.Service
{
    public sealed class OpcUaService : IDisposable
    {
        private readonly object sessionLock = new();
        private Session? session;
        private bool disposed;

        public bool IsConnected
        {
            get
            {
                try
                {
                    lock (sessionLock)
                    {
                        return session != null && session.Connected && !session.KeepAliveStopped;
                    }
                }
                catch (Exception ex)
                {
                    LogService.AddException("OPC UA connection status check failed", ex);
                    return false;
                }
            }
        }

        public async Task ConnectAsync(string endpointUrl)
        {
            if (string.IsNullOrWhiteSpace(endpointUrl))
                throw new InvalidOperationException("OPC UA endpoint URL is empty. Please check Parameters.xml.");

            try
            {
                LogService.AddLog($"OPC UA connection started. Endpoint:{endpointUrl}");
                Disconnect();

                var config = new ApplicationConfiguration
                {
                    ApplicationName = "IOTrim_OPCUA_Client",
                    ApplicationUri = "urn:localhost:IOTrim_OPCUA_Client",
                    ApplicationType = ApplicationType.Client,
                    SecurityConfiguration = new SecurityConfiguration
                    {
                        ApplicationCertificate = new CertificateIdentifier
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/MachineDefault",
                            SubjectName = "IOTrim_OPCUA_Client"
                        },
                        TrustedIssuerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/UA Certificate Authorities"
                        },
                        TrustedPeerCertificates = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/UA Applications"
                        },
                        RejectedCertificateStore = new CertificateTrustList
                        {
                            StoreType = "Directory",
                            StorePath = "OPC Foundation/CertificateStores/RejectedCertificates"
                        },
                        AutoAcceptUntrustedCertificates = true,
                        AddAppCertToTrustedStore = true
                    },
                    TransportConfigurations = new TransportConfigurationCollection(),
                    TransportQuotas = new TransportQuotas { OperationTimeout = 10000 },
                    ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
                };

                await config.Validate(ApplicationType.Client);

                EndpointDescription endpoint = CoreClientUtils.SelectEndpoint(config, endpointUrl, false);
                var endpointConfig = EndpointConfiguration.Create(config);
                var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfig);

                Session newSession = await Session.Create(config, configuredEndpoint, false, "IOTrim OPC UA Client", 60000, null, null);
                newSession.KeepAlive += Session_KeepAlive;

                lock (sessionLock)
                    session = newSession;

                LogService.AddLog($"OPC UA session created. Connected:{IsConnected}");
            }
            catch (Exception ex)
            {
                LogService.AddException($"OPC UA connection failed. Endpoint:{endpointUrl}", ex);
                throw;
            }
        }

        private void Session_KeepAlive(ISession sender, KeepAliveEventArgs e)
        {
            if (ServiceResult.IsBad(e.Status))
                LogService.AddLog($"OPC UA keep-alive warning: {e.Status}");
        }

        public object? ReadValue(string nodeId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(nodeId))
                    throw new ArgumentException("OPC UA node id is empty.", nameof(nodeId));

                Session currentSession;
                lock (sessionLock)
                {
                    currentSession = session ?? throw new InvalidOperationException("OPC UA is not connected.");
                }

                DataValue value = currentSession.ReadValue(new NodeId(nodeId));
                if (StatusCode.IsBad(value.StatusCode))
                    throw new ServiceResultException(value.StatusCode, $"Bad OPC UA status for node {nodeId}");

                return value.Value;
            }
            catch (Exception ex)
            {
                LogService.AddException($"OPC UA read failed. NodeId:{nodeId}", ex);
                throw;
            }
        }

        public void Disconnect()
        {
            Session? oldSession = null;
            try
            {
                lock (sessionLock)
                {
                    oldSession = session;
                    session = null;
                }

                if (oldSession != null)
                {
                    oldSession.KeepAlive -= Session_KeepAlive;
                    oldSession.Close(2000);
                    oldSession.Dispose();
                    LogService.AddLog("OPC UA disconnected.");
                }
            }
            catch (Exception ex)
            {
                LogService.AddException("OPC UA disconnect failed", ex);
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}
