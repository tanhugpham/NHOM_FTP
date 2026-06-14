using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace FileTransfer.Shared.Security
{
    public static class CertificateValidation
    {
        public static bool ValidateServerCertificate(
            X509Certificate2 serverCertificate,
            X509Certificate2 caCertificate,
            string expectedHostname)
        {
            if (serverCertificate == null)
                throw new ArgumentNullException(nameof(serverCertificate));

            if (caCertificate == null)
                throw new ArgumentNullException(nameof(caCertificate));

            if (string.IsNullOrWhiteSpace(expectedHostname))
                throw new ArgumentNullException(nameof(expectedHostname));

            ValidateCertificateNotExpired(serverCertificate);

            ValidateCertificateChain(serverCertificate, caCertificate);

            ValidateNotSelfSigned(serverCertificate);

            ValidateSubjectName(serverCertificate, expectedHostname);

            return true;
        }

        public static bool ValidateClientCertificate(
            X509Certificate2 clientCertificate,
            X509Certificate2 caCertificate)
        {
            if (clientCertificate == null)
                throw new ArgumentNullException(nameof(clientCertificate));

            if (caCertificate == null)
                throw new ArgumentNullException(nameof(caCertificate));

            ValidateCertificateNotExpired(clientCertificate);

            ValidateCertificateChain(clientCertificate, caCertificate);

            ValidateNotSelfSigned(clientCertificate);

            return true;
        }

        public static void ValidateCertificateNotExpired(
            X509Certificate2 certificate)
        {
            DateTime now = DateTime.UtcNow;

            if (now < certificate.NotBefore.ToUniversalTime())
            {
                throw new InvalidOperationException(
                    "Certificate is not yet valid. " +
                    "NotBefore: " + certificate.NotBefore.ToString("yyyy-MM-dd"));
            }

            if (now > certificate.NotAfter.ToUniversalTime())
            {
                throw new InvalidOperationException(
                    "Certificate has expired. " +
                    "NotAfter: " + certificate.NotAfter.ToString("yyyy-MM-dd"));
            }
        }

        public static void ValidateCertificateChain(
            X509Certificate2 certificate,
            X509Certificate2 caCertificate)
        {
            using (X509Chain chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
                chain.ChainPolicy.VerificationFlags =
                    X509VerificationFlags.AllowUnknownCertificateAuthority;

                chain.ChainPolicy.ExtraStore.Add(caCertificate);

                bool isChainValid = chain.Build(certificate);

                if (!isChainValid)
                {
                    string errors = string.Join(
                        "; ",
                        chain.ChainStatus
                            .Select(s => s.StatusInformation));

                    throw new InvalidOperationException(
                        "Certificate chain validation failed: " + errors);
                }

                bool hasTrustedRoot = chain.ChainElements
                    .Cast<X509ChainElement>()
                    .Any(element =>
                        element.Certificate.Thumbprint ==
                        caCertificate.Thumbprint);

                if (!hasTrustedRoot)
                {
                    throw new InvalidOperationException(
                        "Certificate is not signed by the trusted CA. " +
                        "CA thumbprint: " + caCertificate.Thumbprint);
                }
            }
        }

        public static void ValidateNotSelfSigned(
            X509Certificate2 certificate)
        {
            if (certificate.Subject == certificate.Issuer)
            {
                throw new InvalidOperationException(
                    "Self-signed certificates are not allowed. " +
                    "Subject: " + certificate.Subject);
            }
        }

        public static void ValidateSubjectName(
            X509Certificate2 certificate,
            string expectedHostname)
        {
            string cn = certificate.GetNameInfo(
                X509NameType.SimpleName,
                false);

            if (string.IsNullOrWhiteSpace(cn))
            {
                throw new InvalidOperationException(
                    "Certificate has no Common Name (CN).");
            }

            if (!cn.Equals(expectedHostname, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Certificate CN '" + cn + "' " +
                    "does not match expected hostname '" +
                    expectedHostname + "'.");
            }
        }
    }
}