# Secure File Transfer Client-Server

## Project Overview

A real-time file transfer application built with a client-server architecture. Users can securely upload, download, and share files between a WPF client and a WinForms server over TLS-encrypted TCP sockets. All file transfers use AES-256 encryption, and user authentication uses BCrypt password hashing.

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Client UI | WPF (.NET Framework 4.7.2) |
| Server UI | WinForms (.NET Framework 4.7.2) |
| Shared Library | .NET Framework 4.7.2 Class Library |
| Transport | TCP + TLS 1.2 (mTLS) |
| Serialization | JSON (Newtonsoft.Json) |
| Database ORM | Entity Framework Core 3.1.32 |
| Database | PostgreSQL (Render Cloud) |
| Password Hashing | BCrypt.Net-Next 4.2.0 |
| File Encryption | AES-256 (System.Security.Cryptography) |

---

## Solution Structure

```
FileTransferSolution.sln
├── FileTransfer.Client          (WPF - End User)
│   ├── MainWindow.xaml/.cs      Login, Upload, Download, Share
│   ├── RequestsWindow.xaml/.cs  Push Offers (Accept/Reject/Detail)
│   └── Networking/
│       └── TcpClientService.cs  TCP + TLS client
├── FileTransfer.Server          (WinForms - Admin)
│   ├── Form1.cs                 Dashboard, Push Files, Logs
│   ├── Networking/
│   │   └── TcpServer.cs         TCP + TLS server, all handlers
│   ├── Services/                Auth, FileTransferState, History, Share
│   ├── Database/                EF Core AppDbContext
│   └── Entities/                User, ClientSession, TransferHistory...
├── FileTransfer.Shared          (Shared contracts)
│   ├── DTOs/                    All request/response DTOs
│   ├── Enums/                   MessageType
│   ├── Helpers/                 JsonHelper, TcpMessageHelper
│   ├── Protocols/               NetworkMessage
│   ├── Responses/               BaseResponseDto + typed responses
│   └── Security/                AesEncryption, CertificateHelper, Validation
└── docs/                        Documentation
```

---

## Environment Requirements

- **Windows 10/11** (required for WPF/WinForms)
- **Visual Studio 2019+** (required to build Client - `dotnet build` cannot compile WPF)
- **.NET Framework 4.7.2** SDK / Developer Pack
- **PostgreSQL** database (hosted or local)
- **OpenSSL** (for generating certificates)

---

## Setup Order

### 1. Setup Certificates (mTLS)
Follow: `docs/handover/MTLS_SETUP_GUIDE.md`

Generate CA, server, and client certificates. Install them into certificate stores.

### 2. Setup Database
Follow: `docs/handover/DATABASE_SECRET_SETUP.md`

Create PostgreSQL database, run migration script (`FileTransfer.Server/Database/mysql_schema.sql`), configure connection string in `App.config`.

### 3. Setup AES Encryption Key
Follow: `docs/handover/AES_SECRET_SETUP.md`

Generate AES-256 key, add to `App.config` as base64.

### 4. Build & Run
- **Server:** `dotnet build FileTransfer.Server` → `FileTransfer.Server.exe`
- **Client:** Open solution in Visual Studio → Build → Run

---

## Run Instructions

### Server
1. Start `FileTransfer.Server.exe`
2. Enter port (default: 9000)
3. Click **Start Server**
4. Monitor online clients and activity logs

### Client
1. Start `FileTransfer.Client.exe`
2. Enter Server IP and Port
3. Click **Connect**
4. Register or Login
5. Upload, Download, Share files

---

## Important Security Notes

- **mTLS is REQUIRED.** Both client and server must present valid certificates signed by the same CA.
- **AES-256 encryption** is applied to every file chunk before transmission.
- **Passwords are hashed** with BCrypt before storage.
- **No hardcoded secrets.** All credentials, keys, and passwords are stored in `App.config`.
- **Thread safety:** All server collections use `ConcurrentDictionary`. Client uses `SemaphoreSlim` for SSL stream access.

---

## Key Documents

| Document | Location |
|----------|----------|
| System Architecture | `docs/handover/ARCHITECTURE.md` |
| Developer Setup | `docs/handover/DEVELOPER_SETUP.md` |
| Database Setup | `docs/handover/DATABASE_SETUP.md` |
| mTLS Setup Guide | `docs/handover/MTLS_SETUP_GUIDE.md` |
| AES Secret Setup | `docs/handover/AES_SECRET_SETUP.md` |
| Security Architecture | `docs/handover/SECURITY_ARCHITECTURE.md` |
| Features Overview | `docs/handover/FEATURES.md` |
| Network Protocol | `docs/handover/NETWORKING.md` |
| Push Request Flow | `docs/handover/PUSH_REQUEST_FLOW.md` |
| Final System Audit | `docs/context/SYSTEM_FINAL_AUDIT.md` |
| Testing Strategy | `docs/context/TESTING_STRATEGY_AUDIT.md` |
| Technical Debt | `docs/context/TECHNICAL_DEBT.md` |
| Defense Guide | `docs/handover/DEFENSE_GUIDE.md` |
| Class Map | `docs/handover/CLASS_MAP.md` |
| Important Flows | `docs/handover/IMPORTANT_FLOWS.md` |