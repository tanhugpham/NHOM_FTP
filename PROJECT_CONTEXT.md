# Project Summary

- **Tên dự án:** Secure File Transfer Client-Server (FileTransferSolution)
- **Mục tiêu:** Ứng dụng truyền file thời gian thực mô hình Client-Server qua TCP Socket, hỗ trợ upload/download có mã hóa AES, chia sẻ file bằng ShareCode, resume upload khi mất kết nối.
- **Trạng thái hiện tại:** MVP (Minimum Viable Product) - Core features hoạt động, nhiều vấn đề bảo mật nghiêm trọng cần xử lý.

# Architecture

```
┌──────────────────────────────────────────────────────┐
│                  FileTransferSolution                  │
├──────────────┬────────────────┬───────────────────────┤
│  Client(WPF) │  Server(WinForms)│  Shared(Class Lib)  │
├──────────────┼────────────────┼───────────────────────┤
│ MainWindow   │ Form1 (UI)    │ DTOs / Enums / Helper │
│ TcpClientSvc │ TcpServer     │ Protocols / Security  │
└──────┬───────┴──────┬────────┴───────────┬───────────┘
       │              │                    │
       └──────TCP─────┘                    │
              │                            │
       ┌──────▼──────┐               DTO/JSON
       │  WinForms   │               Reference
       │  TcpServer  │
       └──────┬──────┘
              │ EF Core
       ┌──────▼──────┐
       │    MySQL     │
       │ (localhost)  │
       └─────────────┘
```

**Pattern hiện tại:** Layered Architecture (Presentation → Service → Data)
- Client không truy cập trực tiếp database.
- Giao tiếp qua TCP Socket với JSON length-prefix protocol.
- Mỗi user có private storage folder riêng.

# Technology Stack

| Thành phần | Công nghệ | Version |
|---|---|---|
| Language | C# | 7.3+ |
| Runtime | .NET Framework | 4.7.2 |
| Client UI | WPF (XAML) | - |
| Server UI | Windows Forms | - |
| Network | TCP Socket (Length-Prefix) | - |
| Serialization | Newtonsoft.Json | 13.0.4 |
| ORM | Entity Framework Core | 3.1.32 |
| Database Driver | Pomelo.EntityFrameworkCore.MySql | 3.2.4 |
| Database | MySQL 8.0+ (InnoDB, utf8mb4) | - |
| Password Hash | BCrypt.Net-Next | 4.2.0 |
| Encryption | AES (System.Security.Cryptography) | - |
| DI | Microsoft.Extensions.DependencyInjection | 3.1.32 |
| Logging | Microsoft.Extensions.Logging | 3.1.32 |

**⚠ Inconsistency:** README ghi PostgreSQL, code thực tế dùng MySQL.

# Database

**Database:** `transferfile_mysql` (MySQL 8.0+)

## Tables (5)

| Table | Mục đích | Khóa chính | Quan hệ |
|---|---|---|---|
| **Users** | Tài khoản người dùng | Id | PK |
| **ClientSessions** | Theo dõi session login | Id | FK → Users(UserId) ON DELETE CASCADE |
| **FileTransferStates** | Trạng thái upload (resume) | Id | Độc lập |
| **TransferHistories** | Audit log truyền file | Id | Độc lập (lưu Username text) |
| **SharedFiles** | Share code chia sẻ file | Id | Độc lập (lưu OwnerUsername text) |

## Sơ đồ quan hệ

```
Users ──1:N──► ClientSessions (qua UserId)
Các bảng khác độc lập, không có FK (quan hệ logic qua Username text)
```

## Connection String (HARDCODED - CRITICAL)
```
Server=localhost;Port=3306;Database=transferfile_mysql;User=root;Password=091103;SslMode=None;
```

# Core Features

## Authentication
- Register: Kiểm tra username tồn tại → BCrypt.HashPassword → Save User
- Login: BCrypt.Verify → Tạo ClientSession → Lưu vào dictionary in-memory `_clientUsers<TcpClient, string>`
- Session chỉ tồn tại trong RAM, mất khi restart server
- Không có token hay JWT

## File Upload
- Chunked upload (64KB/chunk) với AES encryption
- 4 bước: ResumeCheck → FileStart → FileChunk(N) → FileComplete
- Hỗ trợ resume: kiểm tra FileId trong DB → tiếp tục từ chunk cuối
- File chunks được decrypt và append vào disk

## File Download
- Load toàn bộ file vào memory (`File.ReadAllBytes`)
- Gửi qua JSON response (byte[] serialize)
- ⚠ KHÔNG chunked như upload → nguy cơ OutOfMemory với file lớn

## File Sharing
- Tạo ShareCode: 8 ký tự hex uppercase (GUID.N.ToString().Substring(0,8))
- Yêu cầu AllowedUsername để giới hạn người nhận
- Download shared file: kiểm tra share code → validate username → đọc file từ thư mục owner

## Session Management
- Server lưu mapping TcpClient → Username trong dictionary
- Không có logout API (client disconnect là mất session)
- Không có token expiration
- AdminCleanupService: xóa toàn bộ DB logs

# Important Classes

## Server (FileTransfer.Server)

| Class | File | Trách nhiệm |
|---|---|---|
| **Form1** | Form1.cs | Server dashboard WinForms (990 dòng) - UI, event handlers |
| **TcpServer** | Networking/TcpServer.cs | Core server: TCP listener, message routing, business logic (795 dòng) |
| **AuthService** | Services/AuthService.cs | Register & Login với BCrypt |
| **FileTransferStateService** | Services/FileTransferStateService.cs | CRUD upload progress (resume support) |
| **TransferHistoryService** | Services/TransferHistoryService.cs | Ghi audit log |
| **SharedFileService** | Services/SharedFileService.cs | Tạo & query share code |
| **AdminCleanupService** | Services/AdminCleanupService.cs | Xóa toàn bộ DB logs |
| **AppDbContext** | Database/AppDbContext.cs | EF Core DbContext, 5 DbSet, hardcoded connection string |

## Client (FileTransfer.Client)

| Class | File | Trách nhiệm |
|---|---|---|
| **MainWindow** | MainWindow.xaml.cs | Client UI logic (844 dòng): connect, upload, download, share |
| **TcpClientService** | Networking/TcpClientService.cs | TCP client wrapper: connect, send message, disconnect |

## Shared (FileTransfer.Shared)

| Class | File | Trách nhiệm |
|---|---|---|
| **NetworkMessage** | Protocols/NetworkMessage.cs | Message envelope: { MessageType Type, string JsonBody } |
| **MessageType** | Enums/MessageType.cs | 11 enum values: Ping, Register, Login, FileUpload, FileStart, FileChunk, FileComplete, ResumeCheck, Error, GetFileList, DownloadFile, CreateShareCode, DownloadSharedFile |
| **TcpMessageHelper** | Helpers/TcpMessageHelper.cs | Length-prefix TCP read/write |
| **AesEncryptionHelper** | Security/AesEncryptionHelper.cs | AES encrypt/decrypt (HARDCODED key & IV) |
| **BaseResponseDto** | Responses/BaseResponseDto.cs | { bool Success, string Message } |

## Message Protocol (TCP Length-Prefix)
```
[4 bytes length][UTF-8 JSON string]
JSON = { "Type": 3, "JsonBody": "{...}" }
```

# Current Issues

## 🔴 Critical
1. **MySQL password hardcoded** trong AppDbContext.cs (`root:091103`)
2. **AES key & IV hardcoded** trong AesEncryptionHelper.cs (32-byte key, 16-byte IV)
3. **Credentials đã commit lên GitHub** - cần rotate password ngay

## 🟠 High
4. **Download không chunked** - load entire file vào memory
5. **Server chạy trên UI thread** - `await Task.Run` duy nhất cho TcpServer
6. **Không có unit tests**
7. **.Wait() trên async methods** - nguy cơ deadlock
8. **No Dependency Injection** - services instantiated via `new`
9. **README ghi PostgreSQL nhưng code dùng MySQL**

## 🟡 Medium
10. **No file size validation** - DoS risk
11. **No rate limiting / brute force protection** trên login
12. **No TLS** - plain TCP socket
13. **No logout endpoint** - DisconnectedAt không bao giờ update
14. **Race condition** trên `_uploadingFiles` dictionary (không thread-safe)
15. **3 classes empty/placeholder**: ResumeRequestDto, FileChunkResponseDto, Models/ folder
16. **Share code chỉ 8 ký tự hex** (~4 bytes entropy) - có thể brute force
17. **In-memory session** - mất khi restart server

# Coding Standards

## Naming Convention
- **PascalCase** cho class, method, property
- **_camelCase** cho private fields
- Tên biến/method có thể dùng tiếng Việt (vd: `txtStatus.Text = "Đăng nhập thành công"`)
- Error messages: tiếng Việt

## Layer Responsibility
- **Client**: UI + TCP send/receive → Không business logic
- **Server**: Business logic + Database access → Không UI logic
- **Shared**: DTOs, Enums, Helpers, Protocols → Không phụ thuộc UI/Server/DB

## DTO Usage
- Request DTO: gửi từ Client → Server (trong NetworkMessage.JsonBody)
- Response DTO: gửi từ Server → Client (kế thừa BaseResponseDto)
- Không dùng AutoMapper

## Error Handling
- Exception bắt tại HandleClientAsync → trả về BaseResponseDto { Success = false, Message }
- Không có global exception handler
- Lỗi hiển thị qua MessageBox + log UI

## Logging
- Server: ListBox UI (Form1.lstLogs) qua event OnLog
- Client: ListBox UI (lstLogs) qua AddLog()
- Không có logging framework (Serilog, NLog, etc.)
- Database lưu TransferHistory cho audit

# Development Rules

**AI phải tuân thủ:**

1. **Không thay đổi kiến trúc** hiện tại nếu chưa được yêu cầu.
2. **Không đổi tên public classes** - sẽ break references.
3. **Không đổi protocol TCP** hiện tại (length-prefix format, JSON serialization).
4. **Không thay đổi database schema** nếu chưa được phê duyệt.
5. Luôn ưu tiên **sửa lỗi nhỏ trước khi refactor lớn**.
6. Mọi thay đổi phải **tương thích .NET Framework 4.7.2**.
7. Mọi **package mới phải được giải thích lý do**.
8. **Giữ nguyên message type enum values** (có giá trị số explicit như FileUpload = 3).
9. **Không xóa code đang dùng** - chỉ thêm hoặc replace.
10. **Giữ nguyên response format** - tất cả response phải kế thừa BaseResponseDto.

# Next Priorities

1. **🔴 Move MySQL password ra App.config** (dùng ConfigurationManager, encrypt section)
2. **🔴 Move AES key/IV ra config** hoặc dùng key derivation từ user password
3. **🔴 Rotate MySQL password** (đã leak lên GitHub)
4. **🟠 Implement chunked download** (tương tự upload flow)
5. **🟠 Add file size validation + upload limits**
6. **🟠 Remove .Wait() calls** → async all the way
7. **🟠 Add DI cho Services** (dùng Microsoft.Extensions.DependencyInjection đã có)
8. **🟡 Add rate limiting** cho login endpoint
9. **🟡 Implement logout API** (update DisconnectedAt trong ClientSessions)
10. **🟡 Hoàn thiện placeholder classes** (ResumeRequestDto, FileChunkResponseDto)