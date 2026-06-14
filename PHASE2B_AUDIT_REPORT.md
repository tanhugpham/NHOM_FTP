# Phase 2B Audit Report

## 1. Exact SslProtocols value used

**Server (TcpServer.cs line ~159):**
```csharp
System.Security.Authentication.SslProtocols.Tls12
```
→ TLS 1.2

**Client (TcpClientService.cs):**
```csharp
SslProtocols.Tls12
```
→ TLS 1.2

> Both sides use **TLS 1.2 only** (no fallback to older protocols).

## 2. Exact AuthenticateAsServerAsync parameters

```csharp
await sslStream.AuthenticateAsServerAsync(
    _serverCertificate,          // X509Certificate2 (server.pfx)
    clientCertificateRequired: true,  // REQUIRES client cert
    enabledSslProtocols: SslProtocols.Tls12,
    checkCertificateRevocation: false
);
```

## 3. Exact AuthenticateAsClientAsync parameters

```csharp
await _sslStream.AuthenticateAsClientAsync(
    ip,                                          // server IP/hostname from user input
    new X509CertificateCollection { _clientCertificate }, // client.pfx
    SslProtocols.Tls12,
    checkCertificateRevocation: false
);
```

## 4. Source of hostname used for CN validation

**Client-side CN validation:** The `ValidateServerCertificate()` callback in `TcpClientService.cs` does **NOT** call `CertificateValidation.ValidateSubjectName()`. Currently validates:
- ✅ NotExpired
- ✅ ValidateChain (CA trust)
- ✅ NotSelfSigned
- ❌ **CN match against hostname is MISSING**

The hostname `ip` is only passed to `AuthenticateAsClientAsync` (SslStream base layer does its own CN check if using DNS names).

**Server-side CN validation:** `ValidateClientCertificate()` does NOT call `ValidateSubjectName()` either - acceptable since client certificates often have no meaningful hostname.

## 5. Whether any hostname is hardcoded

**NO.** The only hostname/ip comes from:
- Client: `txtServerIp.Text` (user input in MainWindow)
- Server: `IPAddress.Any` (binds to all interfaces)

## 6. Whether any certificate path is hardcoded

**YES - THREE occurrences (all will be fixed in Phase 2C):**

| File | Line | Hardcoded Path |
|---|---|---|
| `TcpServer.cs` | `_serverCertPath = "Certificates\\server.pfx"` | ✅ Hardcoded |
| `TcpServer.cs` | `_caCertPath = "Certificates\\ca.cer"` | ✅ Hardcoded |
| `TcpClientService.cs` | `_clientCertPath = "Certificates\\client.pfx"` | ✅ Hardcoded |
| `TcpClientService.cs` | `_caCertPath = "Certificates\\ca.cer"` | ✅ Hardcoded |

## 7. Whether any password is hardcoded

**NO.** Certificate passwords are sourced from:
1. Environment variables: `FT_SERVER_CERT_PASSWORD`, `FT_CLIENT_CERT_PASSWORD`
2. Optional method parameter fallback

No password appears in any source file.

## 8. Whether certificate revocation checking is enabled

**NO.** Both sides:
```csharp
checkCertificateRevocation: false
```
Additionally in `CertificateValidation.ValidateCertificateChain()`:
```csharp
chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
```

Revocation checking is intentionally disabled (CRL/OCSP not available for internal CA).

## 9. TLS/mTLS Classification

| Check | Value |
|---|---|
| Server presents certificate? | ✅ **YES** (server.pfx via AuthenticateAsServer) |
| Client validates server cert? | ✅ **YES** (ValidateServerCertificate callback) |
| Client presents certificate? | ✅ **YES** (client.pfx via X509CertificateCollection) |
| Server validates client cert? | ✅ **YES** (clientCertificateRequired=true + ValidateClientCertificate callback) |
| **Classification** | ✅ **TRUE MUTUAL TLS (mTLS)** |

This is **not** TLS-only (one-way). It is **full bidirectional mTLS**.

## 10. Security Concerns Before Phase 2C

| # | Issue | Severity | File | Recommended Fix (Phase 2C) |
|---|---|---|---|---|
| 1 | **Hardcoded certificate paths** | MEDIUM | TcpServer.cs, TcpClientService.cs | Move to App.config `<appSettings>` |
| 2 | **Certificate passwords from env vars** | MEDIUM | CertificateHelper.cs | Acceptable for .NET Framework, but document in MTLS_SETUP_GUIDE |
| 3 | **No CN validation on client side** | MEDIUM | TcpClientService.cs ValidateServerCertificate | Add `CertificateValidation.ValidateSubjectName(cert2, hostname)` |
| 4 | **No revocation check** | LOW | Both + CertificateValidation.cs | Document as known limitation (internal CA, no CRL) |
| 5 | **Relative cert paths** | LOW | Both | Will break if exe runs from different working directory — fix via config with `AppDomain.CurrentDomain.BaseDirectory` |
| 6 | **No certificate expiry warning** | LOW | Both | Consider pre-emptive check on server startup |
| 7 | **SslProtocols enum fully qualified** | LOW | TcpServer.cs line 159 | `System.Security.Authentication.SslProtocols.Tls12` — redundant with `using System.Security.Authentication` |