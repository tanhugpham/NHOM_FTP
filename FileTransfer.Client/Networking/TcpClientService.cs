using FileTransfer.Shared.DTOs;
using FileTransfer.Shared.Enums;
using FileTransfer.Shared.Helpers;
using FileTransfer.Shared.Protocols;
using FileTransfer.Shared.Security;

using System;
using System.Configuration;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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
        private bool _disposed = false;
        private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public bool IsConnected
        {
            get
            {
                return _client != null && _client.Connected;
            }
        }

        public async Task ConnectAsync(string ip, int port)
        {
            _disposed = false;
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

        /// <summary>
        /// Sends a JSON message and returns the response JSON.
        /// Thread-safe via SemaphoreSlim.
        /// </summary>
        public async Task<string> SendMessageAsync(string message)
        {
            await _sendLock.WaitAsync();
            try
            {
                if (_disposed || _sslStream == null)
                    throw new InvalidOperationException("Not connected");

                await TcpMessageHelper.SendStringAsync(_sslStream, message);

                string response =
                    await TcpMessageHelper.ReadStringAsync(_sslStream);

                return response;
            }
            finally
            {
                _sendLock.Release();
            }
        }

        public void Disconnect()
        {
            _disposed = true;

            if (_sslStream != null)
            {
                try { _sslStream.Close(); } catch { }
                _sslStream = null;
            }

            if (_client != null)
            {
                try { _client.Close(); } catch { }
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

        /// <summary>
        /// Resets the disposed flag so the connection can be reused
        /// after a re-connect. Call this before ConnectAsync if reusing
        /// the same TcpClientService instance.
        /// </summary>
        public void ResetForReconnect()
        {
            _disposed = false;
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