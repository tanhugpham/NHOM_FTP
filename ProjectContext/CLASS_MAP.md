# Class Map

## Project Structure Overview

```
FileTransferSolution.sln
├── FileTransfer.Client         (WPF Application)
│   ├── App.cs
│   ├── MainWindow.xaml / .cs
│   └── Networking/
│       └── TcpClientService.cs
│
├── FileTransfer.Server         (WinForms Application)
│   ├── Program.cs
│   ├── Form1.cs / .Designer.cs
│   ├── Database/
│   │   └── AppDbContext.cs
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── ClientSession.cs
│   │   ├── FileTransferState.cs
│   │   ├── TransferHistory.cs
│   │   └── SharedFile.cs
│   ├── Migrations/
│   │   ├── 5 migration files
│   │   └── AppDbContextModelSnapshot.cs
│   ├── Networking/
│   │   └── TcpServer.cs
│   └── Services/
│       ├── AuthService.cs
│       ├── FileTransferStateService.cs
│       ├── TransferHistoryService.cs
│       ├── SharedFileService.cs
│       └── AdminCleanupService.cs
│
└── FileTransfer.Shared         (Class Library)
    ├── DTOs/
    │   ├── RegisterRequestDto.cs
    │   ├── LoginRequestDto.cs
    │   ├── FileStartRequestDto.cs
    │   ├── FileChunkDto.cs
    │   ├── FileCompleteDto.cs
    │   ├── FileInfoDto.cs
    │   ├── FileTransferRequestDto.cs
    │   ├── ResumeCheckRequestDto.cs
    │   ├── ResumeCheckResponseDto.cs
    │   ├── DownloadFileRequestDto.cs
    │   ├── DownloadFileResponseDto.cs
    │   ├── CreateShareCodeRequestDto.cs
    │   ├── CreateShareCodeResponseDto.cs
    │   ├── DownloadSharedFileRequestDto.cs
    │   └── ResumeRequestDto.cs
    ├── Enums/
    │   └── MessageType.cs
    ├── Helpers/
    │   ├── JsonHelper.cs
    │   └── TcpMessageHelper.cs
    ├── Protocols/
    │   └── NetworkMessage.cs
    ├── Responses/
    │   ├── BaseResponseDto.cs
    │   ├── FileChunkResponseDto.cs
    │   └── FileListResponseDto.cs
    └── Security/
        └── AesEncryptionHelper.cs
```

---

## Detailed Class Descriptions

### CLIENT TIER (FileTransfer.Client)

---

#### App (App.xaml / App.xaml.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Client |
| **Type** | WPF Application (partial class) |
| **Responsibility** | Application entry point, lifecycle management |
| **Dependencies** | None |
| **Key Methods** | None (default) |

---

#### MainWindow (MainWindow.xaml)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Client |
| **Type** | WPF Window (XAML) |
| **Responsibility** | Defines the complete UI layout |
| **Dependencies** | None (UI only) |

**UI Structure**:
- `LoginPanel` Grid: Server IP, Port, Username, Password, Connect/Register/Login buttons
- `MainPanel` Grid (hidden until login):
  - Header bar (user info, disconnect)
  - Left panel: Upload controls, Share controls, Receive Shared controls
  - Right panel: File DataGrid, Download controls, Activity Logs list

---

#### MainWindow (MainWindow.xaml.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Client |
| **Type** | WPF Window (code-behind) |
| **Responsibility** | All UI event handlers, orchestrates network calls |
| **Dependencies** | `TcpClientService`, all DTOs, `JsonHelper`, `AesEncryptionHelper` |

**Fields**:
- `_clientService` (TcpClientService) - TCP connection manager
- `_selectedFilePath` (string) - Path to selected file for upload
- `_selectedFiles` (string[]) - Multi-file selection array
- `_isDisconnecting` (bool) - Disconnect in progress flag
- `_currentUsername` (string) - Currently logged-in user

**Key Methods**:
- `btnConnect_Click()` - Connect to server
- `btnRegister_Click()` - Send register request
- `btnLogin_Click()` - Send login request, switch to main panel
- `btnBrowse_Click()` - Open file picker (multi-select)
- `btnUpload_Click()` - Upload all selected files
- `UploadSingleFileAsync()` - Upload one file with chunking + resume
- `btnRefreshFiles_Click()` - Refresh file list from server
- `btnDownload_Click()` - Download selected file
- `btnCreateShareCode_Click()` - Generate share code
- `btnDownloadSharedFile_Click()` - Download shared file
- `btnDisconnect_Click()` - Disconnect and return to login
- `SendRequestAsync()` - Generic request/response handler
- `SendResumeCheckAsync()` - Check upload state for resume
- `SendDownloadRequestAsync()` - Download file request
- `SendCreateShareCodeAsync()` - Create share code request
- `SendDownloadSharedFileAsync()` - Download shared file request
- `CreateStableFileId()` - Generate MD5-based FileId
- `RefreshFileListAsync()` - Fetch and display file list
- `AddLog()` - Add timestamped log to UI

---

#### TcpClientService (Networking/TcpClientService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Client.Networking |
| **Type** | Class |
| **Responsibility** | Manages TCP connection to server (connect, send, receive, disconnect) |
| **Dependencies** | `TcpMessageHelper` |

**Fields**: `_client` (TcpClient), `_stream` (NetworkStream)

**Methods**:
- `ConnectAsync(ip, port)` - Establish TCP connection
- `SendMessageAsync(message)` - Send JSON, wait for response JSON
- `Disconnect()` - Close stream and socket

---

### SERVER TIER (FileTransfer.Server)

---

#### Program (Program.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server |
| **Type** | Static class |
| **Responsibility** | Application entry point |
| **Dependencies** | `Form1` |

---

#### Form1 (Form1.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server |
| **Type** | WinForms Form |
| **Responsibility** | Server dashboard UI, Start/Stop/Restart server, display logs, show status |
| **Dependencies** | `TcpServer`, `AdminCleanupService` |

**Key Fields**: `_server` (TcpServer), `_startedAt` (DateTime?), `_timer` (Timer)

**Key Methods**:
- `BuildModernUi()` - Constructs the entire WinForms dashboard (dark theme)
- `btnStart_Click()` - Start TCP server
- `btnStop_Click()` - Stop TCP server
- `btnRestart_Click()` - Restart server
- `btnOpenStorage_Click()` - Open storage folder in Explorer
- `btnClearLogs_Click()` - Clear UI log list
- `btnClearDbLogs_Click()` - Clear transfer history in database
- `AddLog()` - Thread-safe log addition with Invoke
- `GetLocalIpAddress()` - Get local machine IP
- `Timer_Tick()` - Update uptime display every second

---

#### TcpServer (Networking/TcpServer.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Networking |
| **Type** | Class |
| **Responsibility** | Core server: listen for TCP connections, accept clients, route and handle all protocol messages |
| **Dependencies** | `AuthService`, `TransferHistoryService`, `FileTransferStateService`, `SharedFileService`, `AesEncryptionHelper` |

**Key Fields**:
- `_listener` (TcpListener) - Server socket listener
- `_cts` (CancellationTokenSource) - Server shutdown signal
- `_authService` (AuthService) - Registration and login
- `_historyService` (TransferHistoryService) - Transfer logging
- `_stateService` (FileTransferStateService) - Upload progress tracking
- `_sharedFileService` (SharedFileService) - Share code management
- `_uploadingFiles` (Dictionary<string, string>) - Maps FileId to save path
- `_clientUsers` (Dictionary<TcpClient, string>) - Maps socket to username
- `OnLog` (Action<string>) - Event for UI logging

**Key Methods**:
- `StartAsync(port)` - Start listening on port
- `Stop()` - Stop server
- `HandleClientAsync(client)` - Per-client message loop
- `HandleNetworkMessageAsync(msg, client, ip)` - Message router (switch on Type)
- `HandleRegisterAsync()` - Process registration
- `HandleLoginAsync()` - Process login, create session
- `HandleFileStart()` - Initialize file upload
- `HandleFileChunk()` - Receive encrypted chunk, decrypt, append
- `HandleFileComplete()` - Finalize upload, log history
- `HandleGetFileList()` - List user's files
- `HandleDownloadFile()` - Read file, return bytes
- `HandleResumeCheck()` - Check upload state
- `HandleCreateShareCode()` - Generate share code
- `HandleDownloadSharedFile()` - Validate code, return shared file
- `GetCurrentUsername()` - Lookup username by socket
- `GetUserStorageFolder()` - Get user's private directory

---

#### AppDbContext (Database/AppDbContext.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Database |
| **Type** | DbContext (EF Core) |
| **Responsibility** | Database connection and entity management |
| **Dependencies** | Npgsql, EF Core |

**DbSets**: `Users`, `ClientSessions`, `TransferHistories`, `FileTransferStates`, `SharedFiles`

---

#### User (Entities/User.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Entities |
| **Type** | Entity |
| **Responsibility** | Registered user account |
| **Properties** | Id, Username (string), PasswordHash (string), CreatedAt |

---

#### ClientSession (Entities/ClientSession.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Entities |
| **Type** | Entity |
| **Responsibility** | User login session tracking |
| **Properties** | Id, UserId (FK), ClientIp, ConnectedAt, DisconnectedAt?, IsOnline |

---

#### FileTransferState (Entities/FileTransferState.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Entities |
| **Type** | Entity |
| **Responsibility** | Track upload progress for resume |
| **Properties** | Id, FileId, FileName, TotalBytes, BytesReceived, LastChunkIndex, IsCompleted, UpdatedAt |

---

#### TransferHistory (Entities/TransferHistory.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Entities |
| **Type** | Entity |
| **Responsibility** | Audit log of file transfers |
| **Properties** | Id, Username, FileName, FileSize, TransferType, Status, CreatedAt |

---

#### SharedFile (Entities/SharedFile.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Entities |
| **Type** | Entity |
| **Responsibility** | Share code record for file sharing |
| **Properties** | Id, OwnerUsername, FileName, ShareCode, AllowedUsername, CreatedAt, IsActive |

---

#### AuthService (Services/AuthService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Services |
| **Type** | Class |
| **Responsibility** | User registration and authentication |
| **Dependencies** | `AppDbContext`, BCrypt.Net |

**Methods**:
- `RegisterAsync(username, password)` - Check existence, hash password, save user
- `LoginAsync(username, password, clientIp)` - Verify credentials, create session

---

#### FileTransferStateService (Services/FileTransferStateService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Services |
| **Type** | Class |
| **Responsibility** | Save and retrieve upload progress for resume |
| **Dependencies** | `AppDbContext` |

**Methods**:
- `SaveProgressAsync(fileId, fileName, totalBytes, bytesReceived, lastChunkIndex, isCompleted)` - Upsert transfer state
- `GetByFileId(fileId)` - Get state by file identifier

---

#### TransferHistoryService (Services/TransferHistoryService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Services |
| **Type** | Class |
| **Responsibility** | Log file transfer events to database |
| **Dependencies** | `AppDbContext` |

**Methods**:
- `SaveAsync(username, fileName, fileSize, transferType, status)` - Insert transfer history record

---

#### SharedFileService (Services/SharedFileService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Services |
| **Type** | Class |
| **Responsibility** | Create and validate share codes |
| **Dependencies** | `AppDbContext` |

**Methods**:
- `CreateShareCodeAsync(ownerUsername, fileName, allowedUsername)` - Generate GUID code, save to DB
- `GetByShareCode(shareCode)` - Lookup active share code

---

#### AdminCleanupService (Services/AdminCleanupService.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Server.Services |
| **Type** | Class |
| **Responsibility** | Administrative cleanup operations |
| **Dependencies** | `AppDbContext` |

**Methods**:
- `ClearLogsAsync()` - Delete all TransferHistory records

---

### SHARED LIBRARY (FileTransfer.Shared)

---

#### NetworkMessage (Protocols/NetworkMessage.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Protocols |
| **Type** | Class |
| **Responsibility** | Protocol message envelope (Type + JSON body) |
| **Properties** | Type (MessageType), JsonBody (string) |

---

#### MessageType (Enums/MessageType.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Enums |
| **Type** | Enum |
| **Responsibility** | Defines all protocol message types |
| **Values** | Ping, Register, Login, FileUpload, FileStart, FileChunk, FileComplete, ResumeCheck, Error, GetFileList, DownloadFile, CreateShareCode, DownloadSharedFile |

---

#### JsonHelper (Helpers/JsonHelper.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Helpers |
| **Type** | Static class |
| **Responsibility** | JSON serialization/deserialization wrapper |
| **Dependencies** | Newtonsoft.Json |
| **Methods** | `Serialize(obj)`, `Deserialize<T>(json)` |

---

#### TcpMessageHelper (Helpers/TcpMessageHelper.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Helpers |
| **Type** | Static class |
| **Responsibility** | Length-prefixed TCP message framing |
| **Methods** | `SendStringAsync(stream, message)`, `ReadStringAsync(stream)`, `ReadExactAsync(stream, length)` |

---

#### AesEncryptionHelper (Security/AesEncryptionHelper.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Security |
| **Type** | Static class |
| **Responsibility** | AES-256 encryption/decryption of file chunk data |
| **Dependencies** | System.Security.Cryptography |
| **Methods** | `Encrypt(data)`, `Decrypt(encryptedData)` |
| **Fields** | `Key` (32 bytes, hardcoded), `IV` (16 bytes, hardcoded) |

---

#### BaseResponseDto (Responses/BaseResponseDto.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Responses |
| **Type** | Class |
| **Responsibility** | Base response with Success and Message |
| **Properties** | Success (bool), Message (string) |

---

#### FileListResponseDto (Responses/FileListResponseDto.cs)
| Property | Value |
|----------|-------|
| **Namespace** | FileTransfer.Shared.Responses |
| **Type** | Class (extends BaseResponseDto) |
| **Responsibility** | Response containing list of user's files |
| **Properties** | Files (List<FileInfoDto>) |

---

#### DTO Classes (FileTransfer.Shared.DTOs)

| Class | Purpose | Key Properties |
|-------|---------|----------------|
| `RegisterRequestDto` | Registration request | Username, Password |
| `LoginRequestDto` | Login request | Username, Password |
| `FileStartRequestDto` | Start upload request | FileId, FileName, TotalBytes |
| `FileChunkDto` | Upload chunk | FileId, ChunkData (encrypted byte[]), ChunkIndex, IsLastChunk |
| `FileCompleteDto` | Complete upload | FileId, FileName |
| `FileInfoDto` | File metadata | FileName, FileSize |
| `FileTransferRequestDto` | (Unused) | FileName, FileData |
| `ResumeCheckRequestDto` | Resume query | FileId |
| `ResumeCheckResponseDto` | Resume response (extends BaseResponseDto) | LastChunkIndex, BytesReceived, IsCompleted |
| `DownloadFileRequestDto` | Download request | FileName |
| `DownloadFileResponseDto` | Download response (extends BaseResponseDto) | FileName, FileData (byte[]) |
| `CreateShareCodeRequestDto` | Create share request | FileName, AllowedUsername |
| `CreateShareCodeResponseDto` | Share code response (extends BaseResponseDto) | ShareCode |
| `DownloadSharedFileRequestDto` | Download shared request | ShareCode |
| `ResumeRequestDto` | (Empty, unused) | None |