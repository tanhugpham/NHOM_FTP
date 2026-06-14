using FileTransfer.Shared.Helpers;
using FileTransfer.Shared.Security;

using System;
using System.Configuration;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace FileTransfer.Client.Networking
{
    public class TcpClientService
    {
        private TcpClient _client;
        private SslStream _sslStream;

        private X509Certificate2 _clientCertificate;
        private X509Certificate2 _caCertificate;

        private string _targetHostname;

        public bool IsConnected
        {
            get
            {
                return _client != null && _client.Connected;
            }
        }

        public async Task ConnectAsync(string ip, int port)
        {
            _targetHostname = ip;

            _client = new TcpClient();

            await _client.ConnectAsync(ip, port);

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string clientCertPath = Path.Combine(
                    baseDir,
                    ConfigurationManager.AppSettings["ClientCertificatePath"]);

                string caCertPath = Path.Combine(
                    baseDir,
                    ConfigurationManager.AppSettings["CACertificatePath"]);

                _clientCertificate = CertificateHelper.LoadClientCertificate(
                    clientCertPath);

                _caCertificate = CertificateHelper.LoadCACertificate(
                    caCertPath);
            }
            catch (Exception ex)
            {
                Disconnect();

                throw new InvalidOperationException(
                    "Failed to load client certificate: " + ex.Message);
            }

            NetworkStream networkStream =
                _client.GetStream();

            _sslStream = new SslStream(
                networkStream,
                false,
                ValidateServerCertificate);

            try
            {
                await _sslStream.AuthenticateAsClientAsync(
                    ip,
                    new X509CertificateCollection { _clientCertificate },
                    SslProtocols.Tls12,
                    checkCertificateRevocation: false);
            }
            catch (AuthenticationException authEx)
            {
                Disconnect();

                throw new AuthenticationException(
                    "TLS handshake failed: " + authEx.Message);
            }
            catch (Exception ex)
            {
                Disconnect();

                throw new InvalidOperationException(
                    "TLS connection failed: " + ex.Message);
            }
        }

        public async Task<string> SendMessageAsync(string message)
        {
            await TcpMessageHelper.SendStringAsync(
                _sslStream,
                message);

            string response =
                await TcpMessageHelper.ReadStringAsync(_sslStream);

            return response;
        }

        public void Disconnect()
        {
            if (_sslStream != null)
            {
                _sslStream.Close();
                _sslStream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client = null;
            }

            if (_clientCertificate != null)
            {
                _clientCertificate.Dispose();
                _clientCertificate = null;
            }

            if (_caCertificate != null)
            {
                _caCertificate.Dispose();
                _caCertificate = null;
            }
        }

        private bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
            {
                throw new AuthenticationException(
                    "Server certificate not provided");
            }

            X509Certificate2 cert2 =
                new X509Certificate2(certificate);

            CertificateValidation.ValidateCertificateNotExpired(cert2);

            CertificateValidation.ValidateCertificateChain(
                cert2,
                _caCertificate);

            CertificateValidation.ValidateNotSelfSigned(cert2);

            CertificateValidation.ValidateSubjectName(
                cert2,
                _targetHostname);

            return true;
        }
    }
}