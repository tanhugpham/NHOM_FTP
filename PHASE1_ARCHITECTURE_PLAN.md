# Phase 1: Architecture Plan - mTLS Implementation

## Overview

Implement Mutual TLS (mTLS) with CA-signed certificates for the existing File Transfer Client-Server system. The implementation replaces raw `NetworkStream` with `SslStream` while preserving all existing business logic, DTO contracts, message formats, and database schema.

## Files to Modify

| File | Change Type | Impact |
|---|---|---|
| `FileTransfer.Shared/Helpers/TcpMessageHelper.cs` | **Modify** - Change `NetworkStream` → `Stream` parameters | LOW - only type widening |
| `FileTransfer.Shared/Protocols/NetworkMessage.cs` | NO CHANGE | - |
| `FileTransfer.Server/Networking/TcpServer.cs` | **Modify** - Add SslStream wrapping + cert validation callback | MEDIUM |
| `FileTransfer.Server/App.config` | **Modify** - Add certificate paths/passwords | LOW |
| `FileTransfer.Client/Networking/TcpClientService.cs` | **Modify** - Add SslStream wrapping + cert validation callback | MEDIUM |
| `FileTransfer.Client/App.config` | **Modify** - Add certificate paths/passwords | LOW |
| `FileTransfer.Client/MainWindow.xaml` | NO CHANGE | - |
| `FileTransfer.Client/MainWindow.xaml.cs` | NO CHANGE | - |
| `FileTransfer.Server/Form1.cs` | NO CHANGE | - |
| `FileTransfer.Server/Services/*.cs` | NO CHANGE | - |
| `FileTransfer.Shared/DTOs/*.cs` | NO CHANGE | - |

## Files to Create

| File | Purpose |
|---|---|
| `FileTransfer.Shared/Security/CertificateHelper.cs` | Load X509Certificate2 from file/config with error handling |
| `FileTransfer.Shared/Security/CertificateValidation.cs` | CA chain validation, expiration check, CN matching |
| `MTLS_SETUP_GUIDE.md` | Step-by-step certificate generation + deployment guide |

## Architecture Diagram (mTLS Handshake)

```
┌─────────────────────┐          ┌──────────────────────────┐
│  Client (WPF)       │          │  Server (WinForms)        │
│                      │          │                           │
│  TcpClientService    │          │  TcpServer                │
│  ┌─────────────────┐ │          │  ┌─────────────────────┐ │
│  │ SslStream        │ │          │  │ SslStream            │ │
│  │ AuthenticateAs   │◄══TLS 1.2══►│ AuthenticateAsServer  │ │
│  │ Client(certs)    │ │  mTLS    │  │ (certs + require     │ │
│  │ ValidateServer   │ │          │  │  clientCert=true)    │ │
│  │ Cert(Callback)   │ │          │  │ ValidateClient       │ │
│  └────────┬────────┘ │          │  │ Cert(Callback)       │ │
│           │           │          │  └─────────┬───────────┘ │
│           ▼           │          │            ▼             │
│  ┌─────────────────┐ │          │  ┌─────────────────────┐ │
│  │ TcpMessageHelper │ │          │  │ TcpMessageHelper     │ │
│  │ (Stream params)  │ │          │  │ (Stream params)      │ │
│  └─────────────────┘ │          │  └─────────────────────┘ │
└─────────────────────┘          └──────────────────────────┘

Certificate Flow:
1. Client connects to Server (TCP)
2. Server presents server.pfx certificate
3. Client validates server cert against ca.cer
4. Client presents client.pfx certificate
5. Server validates client cert against ca.cer
6. mTLS established → SslStream used for all communication
7. Existing TcpMessageHelper works unchanged (Stream base class)
```

## TcpMessageHelper Change

**Current:**
```csharp
public static async Task SendStringAsync(NetworkStream stream, string message)
public static async Task<string> ReadStringAsync(NetworkStream stream)
```

**After:**
```csharp
public static async Task SendStringAsync(Stream stream, string message)
public static async Task<string> ReadStringAsync(Stream stream)
```

Rationale: `SslStream` extends `Stream`, not `NetworkStream`. Widening to `Stream` allows both `NetworkStream` (future non-TLS fallback) and `SslStream` to be passed.

## TcpServer Change (Server Side)

**Connection flow after change:**
```
1. TcpClient connected
2. Get NetworkStream from client
3. Load server.pfx certificate
4. Wrap NetworkStream in SslStream
5. Call AuthenticateAsServer(serverCert, requireClientCert=true, TLS12)
6. Server-side callback ValidateClientCertificate:
   - Verify chain against CA root (ca.cer)
   - Check expiration
   - Check CN/O matches expected
7. If validation fails → close connection
8. If OK → use SslStream as transport (replaces NetworkStream)
```

**Existing business logic (HandleClientAsync, HandleNetworkMessageAsync, all Handle* methods):**
- NO changes needed. They already use `NetworkStream stream = client.GetStream()`. We replace with `SslStream`.
- TcpMessageHelper methods accept Stream base class.

## TcpClientService Change (Client Side)

**Connection flow after change:**
```
1. Connect TCP
2. Load client.pfx certificate
3. Load ca.cer for server validation
4. Wrap NetworkStream in SslStream
5. Call AuthenticateAsClient with client certs collection
6. Client-side callback ValidateServerCertificate:
   - Verify chain against CA root (ca.cer)
   - Check expiration
   - Check CN matches server hostname
7. If validation fails → throw exception
8. If OK → use SslStream as transport
```

**Existing methods:**
- `ConnectAsync()`: modified to add SslStream wrapping after TCP connect
- `SendMessageAsync()`: NO CHANGE (uses _stream field, now SslStream)
- `Disconnect()`: NO CHANGE

## Certificate Generation Plan

All certificates generated using a single internal CA:

| Certificate | Type | CN | Purpose |
|---|---|---|---|
| **ca.cer** | CA Root (self-signed) | `CN=FileTransferCA` | Sign all certs, validate trust chain |
| **server.pfx** | Server Identity | `CN=fileserver.local` | Server identity proof to clients |
| **client.pfx** | Client Identity | `CN=filetransfer-client` | Client identity proof to server |

## Validation Rules

| Rule | Server Side | Client Side |
|---|---|---|
| CA chain validation | ✅ Verify client cert chain → ca.cer | ✅ Verify server cert chain → ca.cer |
| Expiration check | ✅ Reject expired client certs | ✅ Reject expired server certs |
| CN match | ✅ Verify client CN prefix | ✅ Verify server CN = hostname |
| Revocation check | ⚠ Optional (CRL/OCSP) | ⚠ Optional (CRL/OCSP) |
| Self-signed rejection | ✅ Only CA-signed accepted | ✅ Only CA-signed accepted |

## Build Strategy

1. **Step 1:** Modify `TcpMessageHelper.cs` (change `NetworkStream` → `Stream`)
2. **Step 2:** Create `CertificateHelper.cs` (load certs from config)
3. **Step 3:** Create `CertificateValidation.cs` (validation callbacks)
4. **Step 4:** Modify `TcpServer.cs` (add SslStream, server cert, client validation)
5. **Step 5:** Modify `TcpClientService.cs` (add SslStream, client cert, server validation)
6. **Step 6:** Update `App.config` files (add cert paths/passwords)
7. **Step 7:** Create `MTLS_SETUP_GUIDE.md`
8. **Step 8:** Build and fix errors

## Risks

| Risk | Mitigation |
|---|---|
| SslStream cannot be serialized like NetworkStream | SslStream extends Stream, all TcpMessageHelper params become Stream |
| Certificate password in config file | Use DPAPI or encrypted config sections (documented in guide) |
| Breaking existing connections | Client must reconnect after server enables TLS |
| Performance: TLS handshake overhead | Acceptable for file transfer (not real-time streaming) |
| CA validation complex on .NET 4.7.2 | Use X509Chain with X509RevocationMode.NoCheck for simplicity |

## Effort Estimate

| Phase | Files | Estimated Time |
|---|---|---|
| TcpMessageHelper change | 1 | 5 min |
| CertificateHelper + Validation | 2 new | 30 min |
| TcpServer modification | 1 | 45 min |
| TcpClientService modification | 1 | 45 min |
| App.config updates | 2 | 10 min |
| MTLS_SETUP_GUIDE | 1 new | 20 min |
| Build + Fix errors | - | 30 min |
| **Total** | **8 files** (5 modified, 3 new) | **~3 hours** |

---

**Do you approve this architecture plan for Phase 1?**  
If approved, I will proceed to implement all code changes sequentially.