# mTLS Setup Guide

## 1. Architecture Overview

This guide explains how to set up Mutual TLS (mTLS) for the File Transfer Client-Server system.

**Components:**
- **CA (Certificate Authority):** Internal CA that signs all certificates
- **Server Certificate (server.pfx):** Proves server identity to clients
- **Client Certificate (client.pfx):** Proves client identity to server
- **CA Certificate (ca.cer):** Used by both sides to validate each other

## 2. TLS Handshake Flow

```
Client                                    Server
  │                                         │
  │──── TCP Connect ──────────────────────▶│
  │                                         │
  │◄── Server presents server.pfx ────────│
  │                                         │
  ├── Validate server cert:
  │   - Not expired?
  │   - Signed by CA (ca.cer)?
  │   - CN matches hostname?
  │   - Not self-signed?
  │                                         │
  │──── Client presents client.pfx ───────▶│
  │                                         │
  │                                         ├── Validate client cert:
  │                                         │   - Not expired?
  │                                         │   - Signed by CA (ca.cer)?
  │                                         │   - Not self-signed?
  │                                         │
  │◄══ SslStream established (mTLS) ──────▶│
  │                                         │
  │──── JSON Messages (encrypted) ────────▶│
```

## 3. mTLS Flow

| Step | Action | Validated By |
|---|---|---|
| 1 | TCP connect | - |
| 2 | Server sends server.pfx | Client validates: expiry, CA chain, self-signed, CN |
| 3 | Client sends client.pfx | Server validates: expiry, CA chain, self-signed |
| 4 | mTLS encrypted session | Both sides |
| 5 | Username/Password login via SslStream | AuthService |

## 4. OpenSSL Commands

### 4.1 Create CA Certificate

```bash
# Generate CA private key (RSA 4096)
openssl genrsa -out ca-key.pem 4096

# Create self-signed CA certificate (valid 10 years)
openssl req -x509 -new -nodes -key ca-key.pem \
    -sha256 -days 3650 \
    -out ca-cert.pem \
    -subj "/C=VN/O=FileTransfer/CN=FileTransferCA"

# Export CA to DER format for .NET
openssl x509 -in ca-cert.pem -out ca.cer -outform DER
```

### 4.2 Create Server Certificate

```bash
# Generate server private key (RSA 2048)
openssl genrsa -out server-key.pem 2048

# Create CSR
openssl req -new -key server-key.pem \
    -out server.csr \
    -subj "/C=VN/O=FileTransfer/CN=fileserver.local"

# Sign with CA (valid 1 year)
openssl x509 -req -in server.csr \
    -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out server-cert.pem \
    -days 365 -sha256

# Export as PFX for .NET (requires password)
openssl pkcs12 -export \
    -in server-cert.pem -inkey server-key.pem \
    -out server.pfx \
    -passout pass:YourServerPassword123
```

### 4.3 Create Client Certificate

```bash
# Generate client private key (RSA 2048)
openssl genrsa -out client-key.pem 2048

# Create CSR
openssl req -new -key client-key.pem \
    -out client.csr \
    -subj "/C=VN/O=FileTransfer/CN=filetransfer-client"

# Sign with CA (valid 1 year)
openssl x509 -req -in client.csr \
    -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out client-cert.pem \
    -days 365 -sha256

# Export as PFX for .NET (requires password)
openssl pkcs12 -export \
    -in client-cert.pem -inkey client-key.pem \
    -out client.pfx \
    -passout pass:YourClientPassword456
```

## 5. Environment Variables

Set these before running the application:

### Windows (Command Prompt)
```cmd
set FT_SERVER_CERT_PASSWORD=YourServerPassword123
set FT_CLIENT_CERT_PASSWORD=YourClientPassword456
```

### Windows (PowerShell)
```powershell
$env:FT_SERVER_CERT_PASSWORD = "YourServerPassword123"
$env:FT_CLIENT_CERT_PASSWORD = "YourClientPassword456"
```

### Persistent (System Environment)
```cmd
setx FT_SERVER_CERT_PASSWORD "YourServerPassword123"
setx FT_CLIENT_CERT_PASSWORD "YourClientPassword456"
```

## 6. Certificate Deployment

### Directory Structure

```
FileTransfer.Server/
├── bin/Debug/
│   ├── Certificates/          ← Place certificates here
│   │   ├── server.pfx
│   │   └── ca.cer
│   └── FileTransfer.Server.exe

FileTransfer.Client/
├── bin/Debug/
│   ├── Certificates/          ← Place certificates here
│   │   ├── client.pfx
│   │   └── ca.cer
│   └── FileTransfer.Client.exe
```

### Configuration (App.config)

**Server (FileTransfer.Server/App.config):**
```xml
<appSettings>
    <add key="ServerCertificatePath" value="Certificates\server.pfx" />
    <add key="CACertificatePath" value="Certificates\ca.cer" />
</appSettings>
```

**Client (FileTransfer.Client/App.config):**
```xml
<appSettings>
    <add key="ClientCertificatePath" value="Certificates\client.pfx" />
    <add key="CACertificatePath" value="Certificates\ca.cer" />
</appSettings>
```

Paths are resolved relative to `AppDomain.CurrentDomain.BaseDirectory` (the bin/Debug folder).

## 7. Testing Procedure

### 7.1 Generate Test Certificates
```bash
# Run all commands from section 4.1-4.3 above
# Copy files:
#   server.pfx + ca.cer → Server/bin/Debug/Certificates/
#   client.pfx + ca.cer → Client/bin/Debug/Certificates/
```

### 7.2 Set Environment Variables
```cmd
set FT_SERVER_CERT_PASSWORD=YourServerPassword123
set FT_CLIENT_CERT_PASSWORD=YourClientPassword456
```

### 7.3 Start Server
1. Run FileTransfer.Server.exe
2. Click "Start Server" on port 9000
3. Verify log: "Server certificate loaded successfully"

### 7.4 Start Client
1. Run FileTransfer.Client.exe
2. Enter Server IP (127.0.0.1 for localhost)
3. Click "Connect to Server"
4. Verify logs show TLS handshake completed

### 7.5 Test File Transfer
1. Register a new user
2. Login
3. Upload a file
4. Verify file appears in server storage
5. Download the file
6. Verify content matches

## 8. Failure Test Cases

| Test Case | Expected Behavior |
|---|---|
| No client certificate provided | TLS handshake rejected, connection closed |
| Expired client certificate | CertificateValidation throws, handshake rejected |
| Wrong CA signer | Chain validation fails, handshake rejected |
| Self-signed client cert | ValidateNotSelfSigned throws, rejected |
| No CA certificate file | Server/Client fails to start (file not found) |
| Wrong certificate password | Certificate loading fails on startup |
| Wrong CN on server cert | Client ValidateSubjectName throws, rejected |
| Server cert not provided | TLS handshake fails on client side |
| Wrong TLS version (SSL3, TLS 1.0/1.1) | Not supported - only TLS 1.2 accepted |
| Revoked certificate | Not checked (RevocationMode.NoCheck) |

## 9. Troubleshooting

### "Failed to load server certificate"
- Check certificate file exists in `bin/Debug/Certificates/`
- Verify `FT_SERVER_CERT_PASSWORD` environment variable is set
- Confirm PFX password matches

### "TLS handshake failed"
- Client certificate may be missing or invalid
- Server certificate may be expired
- Check both sides have same CA (ca.cer)

### "Client certificate validation failed"
- Client cert not signed by the CA
- Client cert is self-signed
- Client cert is expired

### "Certificate chain validation failed"
- CA certificate (ca.cer) does not match the one that signed the certificate
- Regenerate all certificates using the same CA

### "Certificate CN does not match expected hostname"
- Client connects to an IP different from the server certificate's CN
- For testing with `127.0.0.1`, set server cert CN to `127.0.0.1` or use `localhost`

## 10. Security Notes

- **Never** commit certificate files (.pfx, .key, .pem) to Git
- **Never** commit certificate passwords to source code
- Use environment variables for all secrets
- Certificates expire after 1 year (server/client) or 10 years (CA) - set reminders
- For production, use a proper CA (internal or public) instead of self-signed CA
- Consider OCSP/CRL for certificate revocation in production