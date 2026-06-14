# Developer Setup Guide

## One Command Setup

```powershell
.\scripts\setup-dev.ps1
```

Run this from the repository root after cloning. The script will:

1. Generate CA, server, and client certificates
2. Deploy certificates to the correct directories
3. Generate AES-256 encryption key and IV
4. Prompt for MySQL database password (hidden input)
5. Update both `App.config` files with all generated values
6. Validate everything is correct
7. Display a setup summary

## Requirements

| Tool | Purpose | Required |
|---|---|---|
| Windows 10+ | PowerShell certificate APIs | ✅ Required |
| PowerShell 5.1+ | Script execution | ✅ Required |
| Visual Studio 2019+ | Build solution | ✅ Required |
| .NET Framework 4.7.2 | Runtime | ✅ Required |
| MySQL 8.0+ | Database | ✅ Required |
| OpenSSL | Alternative cert generation | ❌ Optional |

## What the Script Does

### 1. Certificates

Created files:

```
FileTransfer.Server/bin/Debug/Certificates/
├── ca.cer          (public CA certificate)
└── server.pfx      (server identity + private key)

FileTransfer.Client/bin/Debug/Certificates/
├── ca.cer          (public CA certificate)
└── client.pfx      (client identity + private key)
```

Certificate details:

| Certificate | Subject | Signed By | Expiry |
|---|---|---|---|
| CA | `CN=FileTransferCA` | Self-signed | 10 years |
| Server | `CN=127.0.0.1` | CA | 1 year |
| Client | `CN=filetransfer-client` | CA | 1 year |

### 2. AES Encryption

Random 32-byte key and 16-byte IV are generated using `RandomNumberGenerator.Create()` and stored as Base64 in App.config.

### 3. Database Password

You are prompted to enter the MySQL password. Input is hidden (`-AsSecureString`). The password is stored in App.config and can be overridden by setting the `FT_DB_PASSWORD` environment variable.

## Manual Configuration

If you prefer not to use the setup script, manually:

### 1. Generate Certificates

See `MTLS_SETUP_GUIDE.md` for OpenSSL commands.

Or use PowerShell:
```powershell
.\generate_certs.ps1
```

### 2. Set Secrets

Update these in `FileTransfer.Server/App.config`:

```xml
<add key="ServerCertPassword" value="YOUR_SERVER_PFX_PASSWORD" />
<add key="DbPassword" value="YOUR_MYSQL_PASSWORD" />
<add key="AesKey" value="YOUR_BASE64_32_BYTE_KEY" />
<add key="AesIV" value="YOUR_BASE64_16_BYTE_IV" />
```

And in `FileTransfer.Client/App.config`:

```xml
<add key="ClientCertPassword" value="YOUR_CLIENT_PFX_PASSWORD" />
<add key="AesKey" value="YOUR_BASE64_32_BYTE_KEY" />
<add key="AesIV" value="YOUR_BASE64_16_BYTE_IV" />
```

### 3. Environment Variables (Override)

All secrets can be overridden by environment variables:

| Config Key | Environment Variable | Purpose |
|---|---|---|
| `ServerCertPassword` | `FT_SERVER_CERT_PASSWORD` | Server PFX password |
| `ClientCertPassword` | `FT_CLIENT_CERT_PASSWORD` | Client PFX password |
| `DbPassword` | `FT_DB_PASSWORD` | MySQL password |
| `AesKey` | `FT_AES_KEY` | AES-256 key (Base64) |
| `AesIV` | `FT_AES_IV` | AES IV (Base64) |

**Priority:** Environment Variable → App.config → Exception

## Verification

### Certificate files exist
```powershell
Test-Path "FileTransfer.Server\bin\Debug\Certificates\server.pfx"
Test-Path "FileTransfer.Client\bin\Debug\Certificates\client.pfx"
```

### No CHANGE_ME placeholders remain
```powershell
Select-String "CHANGE_ME" "FileTransfer.Server\App.config"
Select-String "CHANGE_ME" "FileTransfer.Client\App.config"
```

Both should return no results.

### AES key size is correct
```powershell
[Convert]::FromBase64String("YOUR_AES_KEY").Length  # must be 32
[Convert]::FromBase64String("YOUR_AES_IV").Length   # must be 16
```

## Troubleshooting

| Problem | Solution |
|---|---|
| `New-SelfSignedCertificate` not found | Run on Windows 10+ with admin rights |
| Certificates not deployed | Run script from repository root |
| `CHANGE_ME` still in config | Re-run setup-dev.ps1 |
| MySQL connection refused | Ensure MySQL is running on localhost:3306 |
| TLS handshake fails | Both sides must use the same CA (ca.cer) |
| AES initialization error | Verify AES key is 32 bytes / IV is 16 bytes |
| Build fails MSB4216 | Open solution in Visual Studio, build from IDE (WPF limitation with dotnet CLI) |