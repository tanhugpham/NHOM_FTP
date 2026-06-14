# PROJECT ANALYSIS REPORT

## PROJECT OVERVIEW

**Project Name:** Secure File Transfer Client-Server  
**Code Name:** FileTransferSolution  
**Repository:** github.com/tanhugpham/FTP-Client-to-Server  
**Solution File:** FileTransferSolution.sln  
**Platform:** Windows Desktop (.NET Framework 4.7.2)  
**Language:** C# 7.3 / C# 8.0 features via NuGet  

**Mục tiêu hệ thống:**  
Ứng dụng truyền file thời gian thực mô hình Client-Server, hỗ trợ upload/download file có mã hóa, chia sẻ file bằng mã share code, resume upload khi mất kết nối, và quản lý file cá nhân cho mỗi user.

---

## SYSTEM ARCHITECTURE

```
┌─────────────────────────────────────────────────────────────┐
│                    FileTransferSolution                      │
├─────────────────┬─────────────────┬─────────────────────────┤
│ FileTransfer    │ FileTransfer    │ FileTransfer            │
│ .Client (WPF)   │ .Server(WinForms)│ .Shared (Class Lib)    │
├─────────────────┼─────────────────┼─────────────────────────┤
│ UI Layer:       │ UI Layer:       │ DTOs (13 files)        │
│  - MainWindow   │  - Form1        │ Enums (MessageType)     │
│  (XAML/C#)      │  (WinForms UI)  │ Protocols               │
│                 │                 │  - NetworkMessage       │
│ Networking:     │ Networking:     │ Helpers                 │
│  - TcpClient    │  - TcpServer    │  - JsonHelper           │
│  Service        │  (TCP Listener) │  - TcpMessageHelper     │
│                 │                 │ Responses               │
│ ────────────────│ Services:       │  - BaseResponseDto      │
│ TCP Socket      │  - AuthService  │  - FileChunkResponseDto │
│ (Length-Prefix) │  - Transfer     │  - FileListResponseDto  │
│                 │    HistorySvc   │ Security                │
│ ────────────────│  - FileTransfer │  - AesEncryptionHelper  │
│ Shared Library  │    StateService │─────────────────────────│
│ References      │  - SharedFile   │   Shared Contracts      │
│                 │    Service      │   & Utilities           │
│                 │  - AdminCleanup │                         │
│                 │    Service      │                         │
│                 │                 │                         │
│                 │ Database:       │                         │
│                 │  - AppDbContext │                         │
│                 │  - Entities(5)  │                         │
│                 │  - MySQL Schema │                         │
│                 │                 │                         │
│                 │ Repositories/   │                         │
│                 │ Security/       │                         │
│                 │ Storage Folders │ (empty - planned)       │
└─────────────────┴─────────────────┴─────────────────────────┘
                                 │
                                 ▼
                    ┌─────────────────────┐
                    │   MySQL Database     │
                    │ (transferfile_mysql) │
                    │   Localhost:3306     │
                    │  User: root         │
                    └─────────────────────┘
```

**Luồng dữ liệu chính:**
```
WPF Client ──TCP Socket──▶ WinForms Server ──EF Core──▶ MySQL DB
     ▲                         │
     └──────JSON Response──────┘
```

---

## DIRECTORY STRUCTURE

```
project_Luat/
├── .gitignore
├── FileTransferSolution.sln
├── README.md
├── fixing.txt                              (NextJS errors - unrelated)
├── testing/                                (Test RTF documents)
│   ├── doc1.rtf ~ doc4.rtf
│
├── FileTransfer.Client/                    [WPF Client Application]
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   ├── packages.config
│   ├── App.config
│   ├── FileTransfer.Client.csproj
│   └── Networking/
│       └── TcpClientService.cs
│
├── FileTransfer.Server/                    [WinForms Server Application]
│   ├── Program.cs
│   ├── Form1.cs / Form1.Designer.cs / Form1.resx
│   ├── packages.config
│   ├── App.config
│   ├── FileTransfer.Server.csproj
│   ├── Database/
│   │   ├── AppDbContext.cs
│   │   └── mysql_schema.sql
│   ├── Entities/
│   │   ├── ClientSession.cs
│   │   ├── FileTransferState.cs
│   │   ├── SharedFile.cs
│   │   ├── TransferHistory.cs
│   │   └── User.cs
│   ├── Networking/
│   │   └── TcpServer.cs
│   ├── Services/
│   │   ├── AdminCleanupService.cs
│   │   ├── AuthService.cs
│   │   ├── FileTransferStateService.cs
│   │   ├── SharedFileService.cs
│   │   └── TransferHistoryService.cs
│   ├── Repositories/                       (empty - planned)
│   ├── Security/                           (empty - planned)
│   └── Storage/                            (gitignored - user files)
│
├── FileTransfer.Shared/                    [Shared Class Library]
│   ├── packages.config
│   ├── FileTransfer.Shared.csproj
│   ├── DTOs/
│   │   ├── CreateShareCodeRequestDto.cs
│   │   ├── CreateShareCodeResponseDto.cs
│   │   ├── DownloadFileRequestDto.cs
│   │   ├── DownloadFileResponseDto.cs
│   │   ├── DownloadSharedFileRequestDto.cs
│   │   ├── FileChunkDto.cs
│   │   ├── FileCompleteDto.cs
│   │   ├── FileInfoDto.cs
│   │   ├── FileStartRequestDto.cs
│   │   ├── FileTransferRequestDto.cs        (unused? legacy)
│   │   ├── LoginRequestDto.cs
│   │   ├── RegisterRequestDto.cs
│   │   ├── ResumeCheckRequestDto.cs
│   │   ├── ResumeCheckResponseDto.cs
│   │   └── ResumeRequestDto.cs              (incomplete - empty class)
│   ├── Enums/
│   │   └── MessageType.cs
│   ├── Helpers/
│   │   ├── JsonHelper.cs
│   │   └── TcpMessageHelper.cs
│   ├── Protocols/
│   │   └── NetworkMessage.cs
│   ├── Responses/
│   │   ├── BaseResponseDto.cs
│   │   ├── FileChunkResponseDto.cs          (incomplete - empty class, wrong namespace)
│   │   └── FileListResponseDto.cs
│   ├── Security/
│   │   └── AesEncryptionHelper.cs
│   └── Models/                             (empty - planned)
│
└── ProjectContext/                          (Documentation)
    ├── READ_THIS_FIRST.md
    ├── PROJECT_OVERVIEW.md
    ├── ARCHITECTURE.md
    ├── CLASS_MAP.md
    ├── DATABASE.md
    ├── NETWORKING.md
    ├── FEATURES.md
    ├── IMPORTANT_FLOWS.md
    ├── SECURITY.md
    ├── DEFENSE_GUIDE.md
    └── CHATGPT_CONTEXT_TRANSFER.md
```

---

## TECHNOLOGY STACK

| Thành phần | Công nghệ | Version |
|---|---|---|
| **Ngôn ngữ** | C# | 7.3+ |
| **Runtime** | .NET Framework | 4.7.2 |
| **Client UI** | WPF (Windows Presentation Foundation) | - |
| **Server UI** | WinForms | - |
| **Network Protocol** | TCP Sockets (Length-Prefix Protocol) | - |
| **Serialization** | JSON (Newtonsoft.Json) | 13.0.4 |
| **ORM** | Entity Framework Core | 3.1.32 |
| **Database** | MySQL 8.0+ | via Pomelo 3.2.4 |
| **MySQL Driver** | MySqlConnector | 0.69.10 |
| **Password Hashing** | BCrypt.Net-Next | 4.2.0 |
| **Encryption** | AES (System.Security.Cryptography) | - |
| **DI Container** | Microsoft.Extensions.DependencyInjection | 3.1.32 |
| **Logging** | Microsoft.Extensions.Logging | 3.1.32 |
| **IDE** | Visual Studio 2019 (v16) | - |

**Note:** README ghi "PostgreSQL" nhưng code thực tế dùng MySQL (Pomelo + MySqlConnector). Đây là inconsistency cần fix.

**Package Dependencies:**
- Server: 44 NuGet packages
- Client: 1 NuGet package (Newtonsoft.Json)
- Shared: 1 NuGet package (Newtonsoft.Json)

---

## DATABASE DESIGN

**Database:** `transferfile_mysql` (MySQL 8.0+, InnoDB, utf8mb4)

### Sơ đồ quan hệ (Text ERD)

```
┌──────────────┐       ┌──────────────────┐
│    Users     │       │  ClientSessions   │
├──────────────┤       ├──────────────────┤
│ PK Id (INT)  │◄──────│ FK UserId (INT)   │
│ Username(255)│       │ ClientIp (VARCHAR45)
│ PasswordHash │       │ ConnectedAt (DATETIME)
│  (VARCHAR255)│       │ DisconnectedAt?(DT)
│ CreatedAt    │       │ IsOnline (TINYINT1)
└──────────────┘       └──────────────────┘
       │
       │ (no FK)
       ▼
┌──────────────────┐   ┌──────────────────────┐
│ TransferHistories │   │  FileTransferStates   │
├──────────────────┤   ├──────────────────────┤
│ PK Id (INT)      │   │ PK Id (INT)          │
│ Username (VARCHAR)│  │ FileId (VARCHAR255)   │
│ FileName (VARCHAR)│  │ FileName (VARCHAR255) │
│ FileSize (BIGINT) │  │ TotalBytes (BIGINT)   │
│ TransferType(50)  │  │ BytesReceived (BIGINT)│
│ Status (VARCHAR50)│  │ LastChunkIndex (INT)  │
│ CreatedAt (DT)    │  │ IsCompleted (TINYINT) │
└──────────────────┘  │ UpdatedAt (DATETIME)   │
                       └──────────────────────┘

┌──────────────────┐
│   SharedFiles     │
├──────────────────┤
│ PK Id (INT)      │
│ OwnerUsername     │
│ FileName (VARCHAR)│
│ ShareCode (UNIQUE)│
│ AllowedUsername   │
│ CreatedAt (DT)    │
│ IsActive (TINYINT)│
└──────────────────┘
```

### Tables (5 tables)

| Table | Purpose | Key Columns | Indexes |
|---|---|---|---|
| **Users** | Registered accounts | Id, Username (Unique), PasswordHash, CreatedAt | PK, UQ_Username |
| **ClientSessions** | Login session tracking | Id, UserId (FK→Users), ClientIp, ConnectedAt, DisconnectedAt, IsOnline | PK, IX_UserId |
| **FileTransferStates** | Upload progress (resume) | Id, FileId, FileName, TotalBytes, BytesReceived, LastChunkIndex, IsCompleted | PK, IX_FileId |
| **TransferHistories** | Audit log | Id, Username, FileName, FileSize, TransferType, Status, CreatedAt | PK, IX_Username |
| **SharedFiles** | Share code storage | Id, OwnerUsername, FileName, ShareCode (Unique), AllowedUsername, CreatedAt, IsActive | PK, UQ_ShareCode, IX_ShareCode |

**Connection String (hardcoded):**
```
Server=localhost;Port=3306;Database=transferfile_mysql;User=root;Password=091103;SslMode=None;
```
⚠ **CRITICAL SECURITY ISSUE:** Password hardcoded in source code, committed to GitHub.

---

## BUSINESS FLOW

### 1. User Registration & Login Flow
```
Client                    Server                     Database
  │                         │                          │
  ├── Register(username, pw)│                          │
  │────TCP Socket──────────▶│                          │
  │                         ├── Check username exists  │
  │                         │────EF Core──────────────▶│
  │                         │◀──result─────────────────│
  │                         ├── BCrypt.HashPassword(pw)│
  │                         ├── Save User─────────────▶│
  │◀──JSON Response─────────│                          │
  │                         │                          │
  ├── Login(username, pw)   │                          │
  │────TCP Socket──────────▶│                          │
  │                         ├── Find User─────────────▶│
  │                         ├── BCrypt.Verify(pw)      │
  │                         ├── Create ClientSession─▶│
  │                         ├── Store in _clientUsers  │
  │◀──JSON Response─────────│                          │
  │                         │                          │
  ├── Load MainPanel        │                          │
```

### 2. File Upload Flow (Chunked with Resume)
```
Client                              Server
  │                                   │
  ├── [1] ResumeCheck(FileId)───────▶│
  │◀── ResumeCheckResponse──────────│
  │                                   │
  ├── [2] If new upload:
  │   ├── FileStart(FileId, name,    │
  │   │   totalBytes)───────────────▶│
  │   │                              ├── Create empty file
  │   │                              ├── Save FileTransferState
  │   │◀── BaseResponse(OK)─────────│
  │                                   │
  ├── [3] For each chunk (64KB):
  │   ├── Read chunk from disk
  │   ├── AesEncryptionHelper.Encrypt│
  │   ├── FileChunk(FileId, data,    │
  │   │   index, isLast)───────────▶│
  │   │                              ├── AES.Decrypt(data)
  │   │                              ├── Append to file
  │   │                              ├── Update FileTransferState
  │   │◀── BaseResponse(OK)─────────│
  │                                   │
  ├── [4] FileComplete(FileId, name)▶│
  │   │                              ├── Mark state IsCompleted
  │   │                              ├── Save TransferHistory
  │   │◀── BaseResponse(Upload done)│
  │                                   │
  │   Resume path: Skip FileStart,   │
  │   start from lastChunkIndex + 1  │
```

### 3. File Download Flow
```
Client                              Server
  │                                   │
  ├── GetFileList──────────────────▶│
  │   ├── List files in user folder  │
  │◀── FileListResponseDto──────────│
  │                                   │
  ├── DownloadFile(FileName)───────▶│
  │   ├── Check file exists          │
  │   ├── File.ReadAllBytes (entire!)│
  │   ├── Save TransferHistory       │
  │◀── DownloadFileResponseDto       │
  │   (fileData as byte[])           │
  │                                   │
  ├── SaveFileDialog                 │
  ├── File.WriteAllBytes             │
```

### 4. Share File Flow
```
Client A (Owner)              Server                    Client B (Receiver)
  │                             │                            │
  ├── CreateShareCode(         │                            │
  │   FileName, AllowedUser)──▶│                            │
  │                             ├── Verify file exists      │
  │                             ├── Generate 8-char code    │
  │                             ├── Save SharedFile────────▶│
  │◀── ShareCode="ABC12345"────│                            │
  │ (share code sent manually) │                            │
  │                             │                            │
  │                             │   ├── DownloadSharedFile( │
  │                             │   │   ShareCode)──────────▶│
  │                             │   ├── Validate share code │
  │                             │   ├── Check AllowedUser   │
  │                             │   ├── Read file from      │
  │                             │   │   owner's folder      │
  │                             │   ├── Save TransferHist   │
  │                             │   │◀── FileData──────────│
```

---

## IMPORTANT CLASSES

### FileTransfer.Server (WinForms Server)

| Class | File | Role | Lines |
|---|---|---|---|
| **Form1** | Form1.cs | Server dashboard UI (WinForms) | 990 |
| **TcpServer** | Networking/TcpServer.cs | Core TCP server, message routing | 795 |
| **AuthService** | Services/AuthService.cs | Register/Login with BCrypt | 77 |
| **FileTransferStateService** | Services/FileTransferStateService.cs | Upload progress tracking | 58 |
| **TransferHistoryService** | Services/TransferHistoryService.cs | Audit logging | 39 |
| **SharedFileService** | Services/SharedFileService.cs | Share code CRUD | 55 |
| **AdminCleanupService** | Services/AdminCleanupService.cs | Clear all DB logs | 28 |
| **AppDbContext** | Database/AppDbContext.cs | EF Core DbContext (MySQL) | 21 |

### FileTransfer.Client (WPF Client)

| Class | File | Role | Lines |
|---|---|---|---|
| **MainWindow** | MainWindow.xaml.cs | Main client UI logic | 844 |
| **MainWindow.xaml** | MainWindow.xaml | XAML UI layout | 391 |
| **TcpClientService** | Networking/TcpClientService.cs | TCP client wrapper | 57 |

### FileTransfer.Shared (Shared Library)

| Class | File | Role |
|---|---|---|
| **NetworkMessage** | Protocols/NetworkMessage.cs | Message envelope: Type + JsonBody |
| **MessageType** | Enums/MessageType.cs | 11 message types |
| **JsonHelper** | Helpers/JsonHelper.cs | Newtonsoft.Json wrapper |
| **TcpMessageHelper** | Helpers/TcpMessageHelper.cs | Length-prefix TCP read/write |
| **AesEncryptionHelper** | Security/AesEncryptionHelper.cs | AES encrypt/decrypt (hardcoded key/IV) |
| **BaseResponseDto** | Responses/BaseResponseDto.cs | Success + Message |
| **FileChunkDto** | DTOs/FileChunkDto.cs | FileId + ChunkData + Index |
| **FileStartRequestDto** | DTOs/FileStartRequestDto.cs | FileId + FileName + TotalBytes |

---

## CURRENT IMPLEMENTATION STATUS

### Completed Features ✅
- TCP Server/Client communication (length-prefix protocol)
- User registration with BCrypt password hashing
- User login with session tracking
- File upload with chunking (64KB chunks)
- AES encryption for file chunks (hardcoded key)
- Resume upload support (track via FileTransferStates)
- File download (full file in memory)
- File listing per user
- Share code generation (8-char uppercase GUID)
- Shared file download with username validation
- Transfer history audit logging
- Admin cleanup service (clear all DB logs)
- Dark-themed Server dashboard UI
- Modern WPF Client UI with login/main screens

### Incomplete / Placeholder Items ⚠️
1. **FileTransfer.Shared/DTOs/ResumeRequestDto.cs** - Empty class (wrong namespace too)
2. **FileTransfer.Shared/Responses/FileChunkResponseDto.cs** - Empty class (wrong namespace: FileTransfer.Shared.Enums)
3. **FileTransfer.Shared/Models/** - Empty folder (planned)
4. **FileTransfer.Server/Repositories/** - Empty folder (planned but unused)
5. **FileTransfer.Server/Security/** - Empty folder (planned)
6. **FileTransfer.Client/Properties/** - Auto-generated files only
7. **FileTransfer.Server/Connected Services/** - Empty WCF metadata folder

### Potential Bugs / Issues 🐛
1. **Byte array upload (not chunked) for Download** - DownloadFile loads entire file into memory and sends as single byte[] in JSON
2. **Hardcoded AES key/IV** in source code
3. **Hardcoded MySQL password** in AppDbContext.cs
4. **No session persistence** - In-memory _clientUsers dictionary, lost on restart
5. **No logout endpoint** - Client disconnects but no DisconnectedAt update
6. **TransferHistory FileSize** for upload uses info.Length (not actual received bytes) in HandleFileChunk
7. **Race condition on _uploadingFiles** - Not thread-safe Dictionary
8. **FileStream not disposed** in HandleFileChunk (no using for decryptedChunk - actually it's fine as byte[])
9. **Wait() calls on async methods** in HandleFileStart, HandleFileChunk, etc. - Can cause deadlock
10. **No file size limit validation** - Potential DoS via huge files
11. **No chunk count limit** - Potential memory exhaustion
12. **No authentication on GetFileList** - uses GetCurrentUsername but no token/role validation
13. **No authorization on share code** - OwnerUsername not validated against session
14. **ResumeCheckResponseDto** can bypass the regular response wrapper (returns raw DTO not wrapped in BaseResponseDto from HandleResumeCheck)
15. **Duplicate lines in MainWindow constructor**: `LoginPanel.Visibility = Visibility.Visible;` appears twice
16. **HandleFileComplete's completeSize** reads info.Length which may differ from actual bytes written
17. **FindFilePathByName** searches ALL directories in storage root - could return wrong file

---

## RISKS AND ISSUES

### 🔴 Critical Security Concerns

| ID | Issue | Severity | Location |
|---|---|---|---|
| S1 | **MySQL password hardcoded** (root:091103) | CRITICAL | AppDbContext.cs:17 |
| S2 | **AES key & IV hardcoded** in source | CRITICAL | AesEncryptionHelper.cs:14-32 |
| S3 | **Password committed to GitHub** (in connection string) | CRITICAL | Repository history |
| S4 | No rate limiting on login | HIGH | TcpServer.cs |
| S5 | No brute force protection | HIGH | AuthService.cs |
| S6 | No input validation/sanitization | HIGH | All handlers |
| S7 | Path traversal in username (`Path.GetFileName` only) | MEDIUM | TcpServer.GetUserStorageFolder |
| S8 | No HTTPS/TLS - plain TCP socket | MEDIUM | Client-Server communication |
| S9 | Session token is just in-memory dictionary | MEDIUM | TcpServer._clientUsers |
| S10 | No token expiration | MEDIUM | AuthService.cs |
| S11 | Share code is only 8 chars (8 hex nibbles = 4 bytes entropy) | MEDIUM | SharedFileService.cs |

### 🔴 Performance Issues

| ID | Issue | Severity | Location |
|---|---|---|---|
| P1 | **Entire file loaded into memory** on download | HIGH | TcpServer.HandleDownloadFile:536 |
| P2 | **Server runs on single UI thread** | HIGH | Form1.btnStart_Click uses `await Task.Run` |
| P3 | No connection pooling for DB (new DbContext per request) | MEDIUM | All services |
| P4 | **No chunked download** (unlike upload) | MEDIUM | TcpServer.cs |
| P5 | No file size streaming for large files | MEDIUM | Download handler |

### 🔴 Code Quality Issues

| ID | Issue | Severity |
|---|---|---|
| Q1 | **No Dependency Injection** - services instantiated via `new` | HIGH |
| Q2 | **No interfaces/abstractions** - tight coupling | HIGH |
| Q3 | **.Wait() on async tasks** in synchronous methods = deadlock risk | HIGH |
| Q4 | **Inconsistent naming** (Vietnamese mixed with English) | MEDIUM |
| Q5 | **Single file anti-pattern** - TcpServer.cs is 795 lines | MEDIUM |
| Q6 | **Inconsistent code formatting** (indentation, braces) | LOW |
| Q7 | **No unit tests** | HIGH |
| Q8 | **README says PostgreSQL, code uses MySQL** | MEDIUM |
| Q9 | **Error messages in Vietnamese** mixed with code | LOW |

### Technical Debt Summary
- No unit tests or integration tests
- No logging framework (just UI ListBox)
- No configuration management (hardcoded strings)
- No repository pattern (EF Core queries in services)
- No DTO/entity mapping (AutoMapper or manual)
- No exception handling strategy
- No async all the way (mixed sync/async)
- No connection string in config file
- No SSL/TLS for network communication
- No middleware pipeline

---

## RECOMMENDED NEXT STEPS

### Priority 0 - Immediate Security Fixes
1. **Move MySQL password to App.config** (encrypted or user secrets)
2. **Move AES key/IV to config file** or use derived key from password
3. **Remove connection string from GitHub** (already committed - rotate password)
4. **Add .env or secrets.json to .gitignore** (already has *.env)

### Priority 1 - Architecture Improvements
5. **Implement Dependency Injection** (already have DI packages)
6. **Extract interfaces** for all services (IAuthService, IFileService, etc.)
7. **Implement Repository Pattern** (Repository folder exists, empty)
8. **Add logging framework** (Serilog/NLog) instead of UI ListBox
9. **Move connection string to App.config** with encryption

### Priority 2 - Feature Completeness
10. **Implement chunked download** (not full file in memory)
11. **Complete placeholder classes** (ResumeRequestDto, FileChunkResponseDto)
12. **Add logout endpoint** to update DisconnectedAt
13. **Add file size validation** and upload limits
14. **Add user storage quota** per user

### Priority 3 - Code Quality
15. **Write unit tests** for services
16. **Implement async throughout** (remove .Wait() calls)
17. **Add centralized error handling** middleware
18. **Standardize code formatting** (EditorConfig)
19. **Implement thread-safe collections** for _clientUsers and _uploadingFiles

### Priority 4 - Advanced Features
20. **Add TLS/SSL for TCP socket**
21. **Add admin panel** for user management
22. **Real-time progress via SignalR** or keep-alive events
23. **File versioning** or trash/recycle system
24. **Web-based client** (future migration to ASP.NET Core)

---

**Tổng quan đánh giá:** Project đang ở giai đoạn MVP (Minimum Viable Product) với kiến trúc basic Client-Server TCP. Code có nhiều vấn đề bảo mật nghiêm trọng (hardcoded credentials) cần xử lý ngay. Kiến trúc hiện tại là **Layered Architecture đơn giản** (Presentation - Service - Data), chưa áp dụng Clean Architecture, CQRS, hay các pattern nâng cao. Điểm mạnh là đã có các tính năng core hoàn chỉnh (upload chunked, resume, share code) và code khá dễ đọc với comment tiếng Việt.