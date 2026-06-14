using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace FileTransfer.Shared.Security
{
    public static class CertificateHelper
    {
        public static X509Certificate2 LoadServerCertificate(
            string certPath,
            string password = null)
        {
            if (string.IsNullOrWhiteSpace(certPath))
                throw new ArgumentNullException(nameof(certPath));

            password = password
                ?? Environment.GetEnvironmentVariable("FT_SERVER_CERT_PASSWORD")
                ?? ConfigurationManager.AppSettings["ServerCertPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Server certificate password is required. " +
                    "Set FT_SERVER_CERT_PASSWORD environment variable " +
                    "or add ServerCertPassword to App.config.");
            }

            return new X509Certificate2(certPath, password);
        }

        public static X509Certificate2 LoadClientCertificate(
            string certPath,
            string password = null)
        {
            if (string.IsNullOrWhiteSpace(certPath))
                throw new ArgumentNullException(nameof(certPath));

            password = password
                ?? Environment.GetEnvironmentVariable("FT_CLIENT_CERT_PASSWORD")
                ?? ConfigurationManager.AppSettings["ClientCertPassword"];

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Client certificate password is required. " +
                    "Set FT_CLIENT_CERT_PASSWORD environment variable " +
                    "or add ClientCertPassword to App.config.");
            }

            return new X509Certificate2(certPath, password);
        }

        public static X509Certificate2 LoadCACertificate(string certPath)
        {
            if (string.IsNullOrWhiteSpace(certPath))
                throw new ArgumentNullException(nameof(certPath));

            return new X509Certificate2(certPath);
        }
    }
}