# CHATGPT CONTEXT TRANSFER: Secure File Transfer Client-Server

---

## 1. PROJECT IDENTITY

| Field | Value |
|-------|-------|
| **Project Name** | Secure File Transfer Client-Server |
| **Solution** | FileTransferSolution.sln |
| **Purpose** | Real-time encrypted file upload/download/sharing via TCP client-server model |
| **Status** | Production-ready / Completed (all features functional) |
| **Framework** | .NET Framework 4.7.2 |
| **Client UI** | WPF (XAML) |
| **Server UI** | WinForms (Form) |
| **Database** | PostgreSQL (Render.com cloud) |
| **ORM** | Entity Framework Core 3.1.32 |
| **Transport** | Raw TCP Sockets with length-prefix JSON protocol |
| **Password Security** | BCrypt.Net-Next 4.2.0 |
| **File Encryption** | AES-256 (System.Security.Cryptography) |
| **Serialization** | Newtonsoft.Json 13.0.4 |
| **IDE** | Visual Studio 2019 |

---

## 2. ARCHITECTURE SUMMARY

### Overall Structure (3-Tier)

```
┌─────────────────┐     TCP Socket      ┌──────────────────┐     EF Core     ┌────────────┐
│  WPF Client     │◄───────────────────►│  WinForms Server  │───────────────►│ PostgreSQL │
│  (Presentation) │    JSON Messages    │  (Application)    │                │  (Data)    │
└─────────────────┘                     └──────────────────┘                └────────────┘
```

### Client (FileTransfer.Client)
- **WPF Application** with two screens: Login panel + Main dashboard
- **MainWindow.xaml** defines all UI (connect/login forms, upload controls, file grid, share controls, logs)
- **MainWindow.xaml.cs** handles all button events and orchestrates network calls
- **TcpClientService.cs** manages TCP connection (connect, send/receive, disconnect)
- References **FileTransfer.Shared** for DTOs, encryption, protocol types

### Server (FileTransfer.Server)
- **WinForms Application** with dark-theme dashboard (status, logs, controls)
- **TcpServer.cs** core networking: listens on port, accepts clients, routes messages
- **Services layer**: AuthService, FileTransferStateService, TransferHistoryService, SharedFileService, AdminCleanupService
- **Entities**: User, ClientSession, FileTransferState, TransferHistory, SharedFile
- **AppDbContext.cs**: EF Core context with PostgreSQL connection
- **Storage/**: Per-user private folders on disk (gitignored)
- References **FileTransfer.Shared** for DTOs, encryption, protocol types

### Shared Library (FileTransfer.Shared)
- **Class Library** referenced by both Client and Server
- **NetworkMessage** envelope: {MessageType Type, string JsonBody}
- **MessageType enum**: Ping, Register, Login, FileStart, FileChunk, FileComplete, ResumeCheck, Error, GetFileList, DownloadFile, CreateShareCode, DownloadSharedFile
- **DTOs**: 14 request/response data transfer objects
- **Helpers**: JsonHelper, TcpMessageHelper (length-prefix framing)
- **Security**: AesEncryptionHelper (static AES-256 key/IV)
- **Responses**: BaseResponseDto, FileListResponseDto, DownloadFileResponseDto, ResumeCheckResponseDto, CreateShareCodeResponseDto

### Database (PostgreSQL)
- **5 tables**: Users, ClientSessions, FileTransferStates, TransferHistories, SharedFiles
- Hosted on Render.com (Singapore region)
- 5 EF Core migrations applied
- No stored procedures or views

---

## 3. CORE FEATURES

| # | Feature | Purpose | Status | Main Classes |
|---|---------|---------|--------|-------------|
| 1 | TCP Connection | Establish persistent socket to server | Complete | MainWindow, TcpClientService, TcpServer |
| 2 | User Registration | Create account with BCrypt-hashed password | Complete | MainWindow, AuthService |
| 3 | User Login | Authenticate, create session, switch UI | Complete | MainWindow, AuthService, ClientSession |
| 4 | Browse Files | Multi-file selection via OpenFileDialog | Complete | MainWindow |
| 5 | Chunked Upload | Split file into 64KB chunks, encrypt, send | Complete | MainWindow, TcpServer, FileTransferStateService, AesEncryptionHelper |
| 6 | Resume Upload | Continue interrupted uploads from last chunk | Complete | MainWindow, TcpServer, FileTransferStateService |
| 7 | File List | Display user's stored files in DataGrid | Complete | MainWindow, TcpServer |
| 8 | File Download | Download files from server to local machine | Complete | MainWindow, TcpServer, TransferHistoryService |
| 9 | Create Share Code | Generate 8-char code for file sharing | Complete | MainWindow, TcpServer, SharedFileService |
| 10 | Download Shared File | Receive shared file via share code | Complete | MainWindow, TcpServer, SharedFileService |
| 11 | Disconnect | Graceful disconnect, return to login | Complete | MainWindow, TcpClientService, TcpServer |
| 12 | Server Dashboard | Admin monitoring with start/stop/logs | Complete | Form1, TcpServer, AdminCleanupService |
| 13 | Activity Logging | Real-time logs on both UIs | Complete | MainWindow.AddLog(), Form1.AddLog() |
| 14 | Transfer History | Persistent audit log of all transfers | Complete | TransferHistoryService |
| 15 | FileId Generation | MD5-based unique file identifier for resume | Complete | MainWindow.CreateStableFileId() |

---

## 4. DATABASE SUMMARY

### Table: Users
- **Purpose**: Registered user accounts
- **Key columns**: Id (PK), Username, PasswordHash (BCrypt), CreatedAt
- **Relations**: 1:N to ClientSessions

### Table: ClientSessions
- **Purpose**: Login session tracking
- **Key columns**: Id (PK), UserId (FK→Users), ClientIp, ConnectedAt, DisconnectedAt (always null), IsOnline (always true)
- **Issue**: Sessions never closed (DisconnectedAt never set, IsOnline never false)

### Table: FileTransferStates
- **Purpose**: Upload progress tracking for resume
- **Key columns**: Id (PK), FileId (MD5 hash), FileName, TotalBytes, BytesReceived, LastChunkIndex, IsCompleted, UpdatedAt

### Table: TransferHistories
- **Purpose**: Audit log of all file transfers
- **Key columns**: Id (PK), Username (string, not FK), FileName, FileSize, TransferType (Upload/Download/DownloadShared), Status, CreatedAt
- **Note**: No FK to Users - uses string username

### Table: SharedFiles
- **Purpose**: Share code records
- **Key columns**: Id (PK), OwnerUsername (string), FileName, ShareCode (8-char GUID), AllowedUsername, CreatedAt, IsActive (always true)
- **Issue**: Codes never deactivated (IsActive never set to false)

### Migrations (chronological)
1. `20260523073319_InitialPostgresCreate` - Users table
2. `20260523075039_AddClientSessions` - ClientSessions + FK
3. `20260523090715_AddTransferHistories` - TransferHistories
4. `20260523092228_AddFileTransferStates` - FileTransferStates
5. `20260523112931_AddSharedFiles` - SharedFiles

### Connection
- Host: `dpg-d88leh6gvqtc73b7osb0-a.singapore-postgres.render.com`
- Database: `transferfile`
- SSL Mode: Require
- Connection string **hardcoded** in AppDbContext.cs (security issue)

---

## 5. NETWORK PROTOCOL SUMMARY

### Protocol: Length-Prefixed JSON over TCP

```
[4-byte length (Int32)] [UTF-8 JSON payload]
```

### Message Structure
```json
{
  "Type": "Login",
  "JsonBody": "{\"Username\":\"john\",\"Password\":\"secret\"}"
}
```

### Message Types
Client→Server: Register, Login, FileStart, FileChunk, FileComplete, ResumeCheck, GetFileList, DownloadFile, CreateShareCode, DownloadSharedFile
Server→Client: All responses are serialized DTOs (BaseResponseDto or subclasses)

### Flow
1. Client connects via TCP
2. Client sends JSON NetworkMessage
3. Server receives, deserializes, routes by MessageType switch
4. Server processes and returns exactly one response JSON
5. Client deserializes response

### Framing Details
- **Send**: JSON→UTF8 bytes → prepend BitConverter.GetBytes(length) → write both
- **Receive**: Read 4 bytes → Int32 length → read exactly N bytes → UTF8→string → parse JSON
- Solves TCP message boundary problem

### Key Point
Protocol is **synchronous request-response** (not streaming). One request, one response. No TLS/SSL on the TCP connection.

---

## 6. SECURITY SUMMARY

### Authentication (BCrypt)
- **AuthService.RegisterAsync()**: Hashes password with `BCrypt.HashPassword()` before storing
- **AuthService.LoginAsync()**: Verifies with `BCrypt.Verify(password, hash)`
- **Weakness**: Passwords sent as **plaintext JSON** over TCP (no TLS)

### File Encryption (AES-256)
- **AesEncryptionHelper**: Static class with hardcoded 32-byte Key and 16-byte IV
- **Encrypt()**: Called client-side on each 64KB chunk before sending
- **Decrypt()**: Called server-side after receiving each chunk
- **Storage**: Files stored in **plaintext** on server disk (encryption only protects transit)
- **Critical Weakness**: Key and IV are hardcoded in source - anyone with code can decrypt

### Access Control
- Each user gets private storage folder: `/Storage/{username}/`
- Username sanitized with `Path.GetFileName()` (prevents path traversal)
- Server maps TcpClient→username via `Dictionary<TcpClient, string>` (set during login)
- Share codes restricted to specific recipient via `AllowedUsername` check

### Session Management
- Login creates `ClientSession` record (UserId, ClientIp, ConnectedAt, IsOnline=true)
- **Weakness**: No session tokens, no JWT, no timeout, DisconnectedAt never set
- Session is purely socket-based (if socket closes, session dies)

### Known Security Weaknesses
| Issue | Severity | Details |
|-------|----------|---------|
| Plaintext passwords on network | Critical | No TLS, passwords visible to sniffers |
| Hardcoded AES keys | Critical | Key/IV in source code, anyone can decrypt |
| No login rate limiting | High | Brute force possible |
| Sessions never closed | Medium | DisconnectedAt always null |
| Share codes never expire | Medium | IsActive never set to false |
| Connection string in source | High | Database credentials exposed |

---

## 7. IMPORTANT FLOWS (Compact)

### Login Flow
```
User enters IP:Port → Click Connect → TCP handshake → Status "Connected"
→ Enter Username+Password → Click Login → Client sends LoginRequestDto
→ Server AuthService.LoginAsync() → BCrypt.Verify() → Create ClientSession
→ Map socket→username in dictionary → Return success → UI switches to main panel
```

### Upload Flow
```
Select files → Click Upload → For each file:
  Generate FileId = MD5(name|size|timestamp)
  → ResumeCheck → Server returns last chunk index or "new"
  → FileStart → Server creates empty file + DB state
  → LOOP: Read 64KB → AES Encrypt → FileChunk JSON → Server decrypts + appends + updates DB
  → FileComplete → Server marks complete + logs to TransferHistory
  → Refresh file list
```

### Download Flow
```
Select file in grid → Click Download → SaveFileDialog → Send DownloadFileRequest
→ Server reads bytes from user's storage → Logs to TransferHistory
→ Returns DownloadFileResponse(FileName, FileData)
→ Client File.WriteAllBytes() → Progress 100%
```

### Share Flow
```
Select file → Enter recipient username → Click Create Share Code
→ Server generates 8-char GUID → Saves SharedFile to DB → Returns code
→ Owner sends code to recipient (external)
→ Recipient enters code → Click Download Shared File
→ Server validates code + AllowedUsername → Reads from owner's storage → Returns file
```

### Disconnect Flow
```
Click Disconnect → Stream.Close() → Socket.Close() → Server detects close
→ Server removes from _clientUsers → UI returns to login panel
```

---

## 8. IMPORTANT CLASSES (Top 20)

| # | Class | Purpose | Used By |
|---|-------|---------|---------|
| 1 | **MainWindow** (Client) | All UI logic, event handlers, network orchestration | App.xaml |
| 2 | **TcpClientService** (Client) | TCP connection management | MainWindow |
| 3 | **TcpServer** (Server) | TCP listener, accept clients, message routing, all handlers | Form1 |
| 4 | **Form1** (Server) | WinForms dashboard UI, start/stop/restart | Program |
| 5 | **AuthService** (Server) | BCrypt registration and login | TcpServer |
| 6 | **FileTransferStateService** (Server) | Upload progress persistence for resume | TcpServer |
| 7 | **TransferHistoryService** (Server) | Audit log insertion | TcpServer |
| 8 | **SharedFileService** (Server) | Share code CRUD | TcpServer |
| 9 | **AdminCleanupService** (Server) | Clear transfer history | Form1 |
| 10 | **AppDbContext** (Server) | EF Core database context | All Services |
| 11 | **NetworkMessage** (Shared) | Protocol envelope (Type + JsonBody) | Client & Server |
| 12 | **MessageType** (Shared) | Enum of all message types | Client & Server |
| 13 | **JsonHelper** (Shared) | JSON serialize/deserialize wrapper | Client & Server |
| 14 | **TcpMessageHelper** (Shared) | Length-prefix TCP framing | Client & Server |
| 15 | **AesEncryptionHelper** (Shared) | AES-256 encrypt/decrypt | Client & Server |
| 16 | **BaseResponseDto** (Shared) | Base response {Success, Message} | Client & Server |
| 17 | **FileChunkDto** (Shared) | Chunk data with FileId, encrypted bytes, index | Client & Server |
| 18 | **DownloadFileResponseDto** (Shared) | Response with FileName + FileData bytes | Client & Server |
| 19 | **User** (Server Entity) | User account model | AuthService, AppDbContext |
| 20 | **SharedFile** (Server Entity) | Share code model | SharedFileService, AppDbContext |

---

## 9. KNOWN ISSUES

### Critical
1. **Connection string hardcoded in AppDbContext.cs** - Real database credentials (host, username, password) exposed in source code. Anyone with repository access can connect to the production database.
2. **Passwords transmitted in plaintext** over TCP. No TLS/SSL on the socket. Network sniffers can capture login credentials.
3. **AES encryption key and IV hardcoded** in AesEncryptionHelper.cs. Static key means anyone with source code or decompiled binaries can decrypt all file transfers.

### Medium
4. **No login rate limiting** - Attackers can brute-force passwords without throttling.
5. **Client sessions never closed** - `DisconnectedAt` is always null, `IsOnline` is always true. Sessions table becomes inaccurate.
6. **Share codes never expire** - `IsActive` is always set to true and never changed to false. No way to revoke a share code.
7. **Server.Stop() doesn't close existing client connections** - When server stops, connected clients remain in ESTABLISHED state until TCP timeout.
8. **Ping message type defined but never used** - No heartbeat/keep-alive mechanism. Idle connections are never detected.

### Low
9. **Inconsistent timestamp usage** - Some entities use `DateTime.Now` (local), others use `DateTime.UtcNow`.
10. **Missing FK relationships** - TransferHistories and SharedFiles reference users by string username rather than foreign key.
11. **ResumeRequestDto is empty** - File exists but has no properties and is never used.
12. **FileChunkResponseDto is in wrong namespace** - Declared as `FileTransfer.Shared.Enums` instead of `FileTransfer.Shared.Responses`.
13. **Two different GetStorageFolder() methods** - Form1 has one path, TcpServer has another. Could cause confusion.
14. **TransferHistory always records "Success"** - No failure status ever recorded.

---

## 10. CURRENT DEVELOPMENT STATE

### Completed (100%)
- TCP connection/disconnection
- User registration with BCrypt
- User login with session creation
- File upload with 64KB chunking
- File upload resume (persistent state in DB)
- Multi-file selection and upload
- Progress bars (upload and download)
- File list retrieval from server
- File download to local machine
- Share code creation (8-char GUID)
- Shared file download with username validation
- Server dashboard (start, stop, restart, logs)
- Activity logging on both UIs
- Transfer history persistence
- Activity logs clearing (admin)
- AES encryption on file chunks
- Path traversal prevention (Path.GetFileName)

### Partially Completed
- **Session management**: Login creates session, but disconnect never marks offline
- **FileId generation**: MD5-based (works but could use better collision resistance)

### Not Implemented (Defined but unused or missing)
- **Ping/Heartbeat**: MessageType.Ping defined but never sent
- **FileUpload message type**: Defined but unused (replaced by FileStart/Chunk/Complete)
- **ResumeRequestDto**: Empty class file exists but never used
- **FileChunkResponseDto**: Exists but never used (server returns BaseResponseDto for chunks)
- **Share code revocation**: No way to deactivate a share code
- **Rate limiting**: No protection against brute force
- **TLS/SSL**: No transport layer security
- **Unit tests**: No test project exists
- **Configuration management**: No app settings for connection strings

---

## 11. HOW TO HELP THIS PROJECT

With this context document, ChatGPT is equipped to:

### Review Code
- Understand all layers (Client UI, Server logic, Database, Networking)
- Identify anti-patterns, security flaws, and architectural issues
- Evaluate the correctness of the TCP protocol implementation
- Review EF Core usage and database design

### Add Features
- **File sharing enhancements**: Add share code expiration, one-time use, email delivery
- **Admin features**: User management (list, delete, disable), storage quota, bandwidth monitoring
- **Client features**: Folder upload, drag-and-drop, search/filter files, file preview, download queue
- **Server features**: Broadcast messages, maintenance mode, backup/restore storage
- **Protocol features**: Add Ping/Heartbeat for connection health monitoring

### Fix Bugs
- **Session cleanup**: Set DisconnectedAt and IsOnline=false on disconnect
- **Share code management**: Add deactivation endpoint
- **Server Stop cleanup**: Close all client connections during shutdown
- **Timestamp consistency**: Standardize on DateTime.UtcNow everywhere
- **Empty/placeholder classes**: Remove or implement properly

### Redesign Architecture
- **Add dependency injection** for testability
- **Implement repository pattern** for database access
- **Add TLS/SSL** to TCP connections (or wrap with SslStream)
- **Replace hardcoded AES keys** with Diffie-Hellman key exchange
- **Add JWT-based session tokens** instead of socket dictionary
- **Move to .NET 6/8** for cross-platform support
- **Split server UI from server logic** (separate the WinForms from the TCP engine)

### Improve Security (Priority Order)
1. **Add TLS/SSL** to TCP connection (encrypts all traffic including passwords)
2. **Implement per-session encryption keys** (remove hardcoded AES key/IV)
3. **Move connection string** to config file (remove from source)
4. **Add rate limiting** on login endpoint (prevent brute force)
5. **Implement session timeout** (auto-disconnect idle users)
6. **Add share code expiration** (one-time use or time-bound)
7. **Add file integrity verification** (SHA-256 hash after upload)
8. **Use SecureString** for password handling in memory

---

## 12. CHATGPT HANDOVER SUMMARY
### (Paste this section into a new ChatGPT conversation)

```
I am working on a C# .NET Framework 4.7.2 project called "Secure File Transfer Client-Server".

It is a real-time encrypted file transfer application using:
- WPF Client (FileTransfer.Client) for the UI
- WinForms Server (FileTransfer.Server) for the backend
- Shared class library (FileTransfer.Shared) for DTOs and encryption
- Raw TCP Sockets with length-prefix JSON protocol for communication
- Entity Framework Core 3.1.32 with PostgreSQL (hosted on Render.com)
- BCrypt for password hashing
- AES-256 for file chunk encryption

The architecture is 3-tier: Client → TCP → Server → EF Core → PostgreSQL

The server listens on port 9000, accepts multiple clients asynchronously, and routes messages by MessageType enum (Register, Login, FileStart, FileChunk, FileComplete, ResumeCheck, GetFileList, DownloadFile, CreateShareCode, DownloadSharedFile).

Files are uploaded in 64KB encrypted chunks with resume support (server persists progress to FileTransferStates table). Each user has private storage on disk under /Storage/{username}/.

Key classes:
- MainWindow.xaml.cs: Client UI event handlers
- TcpServer.cs: Server networking and message routing
- AuthService.cs: BCrypt registration/login
- TcpMessageHelper.cs: Length-prefix TCP framing
- AesEncryptionHelper.cs: AES-256 encrypt/decrypt (hardcoded keys - known issue)
- AppDbContext.cs: EF Core context (connection string hardcoded - known issue)

Database has 5 tables: Users, ClientSessions, FileTransferStates, TransferHistories, SharedFiles.

Critical issues: Credentials in source code, no TLS (passwords in plaintext), hardcoded AES keys, no rate limiting.

I need help with: [Describe your specific request here - code review, bug fix, feature addition, security improvement, or architecture redesign]
```

---

## FILE INFORMATION

| Field | Value |
|-------|-------|
| **File created** | ProjectContext/CHATGPT_CONTEXT_TRANSFER.md |
| **Estimated token size** | ~5,000-6,000 tokens |
| **Based on** | All 10 files under /ProjectContext/ + full source code analysis |

### Recommended Sections to Paste First
1. **Section 12 (Handover Summary)** - For immediate context in a new ChatGPT conversation
2. **Section 1 (Project Identity)** + **Section 2 (Architecture Summary)** - For structural understanding
3. **Section 11 (How to Help This Project)** - For task-specific guidance
4. **Section 9 (Known Issues)** - If the task involves bug fixing or security review
5. **Section 8 (Important Classes)** + **Section 5 (Network Protocol)** - If the task involves code changes