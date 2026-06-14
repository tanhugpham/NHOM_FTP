# Architecture Documentation

## Overall Architecture

The application follows a **3-tier Client-Server architecture** with a shared library layer:

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT TIER                                  │
│           FileTransfer.Client (WPF Application)                      │
│                                                                     │
│  ┌──────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │
│  │ MainWindow    │  │ TcpClientService│  │ JSON Serialization      │ │
│  │ (UI Layer)    │──│ (Network Layer) │──│ (Shared Library)        │ │
│  │ - XAML UI     │  │ - TCP Connect   │  │ - DTOs                  │ │
│  │ - Event Handlers│ │ - Send/Receive  │  │ - NetworkMessage        │ │
│  └──────────────┘  └─────────────────┘  └─────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                  TCP Socket (Length-prefix)
                           │
┌──────────────────────────▼──────────────────────────────────────────┐
│                        SERVER TIER                                  │
│         FileTransfer.Server (WinForms Application)                  │
│                                                                     │
│  ┌──────────────┐  ┌─────────────────┐  ┌─────────────────────────┐ │
│  │  Form1 (UI)   │  │   TcpServer     │  │   Services Layer       │ │
│  │  - Dashboard  │──│  - Listen       │──│   - AuthService        │ │
│  │  - Logs       │  │  - Accept       │  │   - TransferHistory    │ │
│  │  - Controls   │  │  - Route Msgs   │  │   - FileTransferState  │ │
│  └──────────────┘  └─────────────────┘  │   - SharedFileService   │ │
│                                          │   - AdminCleanupService │ │
│                                          └───────────┬─────────────┘ │
│                                                      │               │
│                                          ┌───────────▼─────────────┐ │
│                                          │   AppDbContext (EF Core) │ │
│                                          │   - DbSets              │ │
│                                          │   - PostgreSQL Provider  │ │
│                                          └─────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────────┘
                           │
                  Entity Framework Core
                           │
┌──────────────────────────▼──────────────────────────────────────────┐
│                     DATABASE TIER                                    │
│           PostgreSQL (Cloud - Render.com)                            │
│                                                                     │
│  ┌──────────┐  ┌──────────────┐  ┌────────────────┐                │
│  │  Users   │  │ClientSessions│  │TransferHistories│                │
│  └──────────┘  └──────────────┘  └────────────────┘                │
│  ┌──────────────────┐  ┌────────────┐                               │
│  │FileTransferStates │  │SharedFiles │                               │
│  └──────────────────┘  └────────────┘                               │
└─────────────────────────────────────────────────────────────────────┘
```

## Client Side (FileTransfer.Client)

### Technology
- **WPF (.NET Framework 4.7.2)**
- References: FileTransfer.Shared library

### Components

| Component | File | Responsibility |
|-----------|------|---------------|
| **Application Entry** | App.xaml / App.xaml.cs | WPF application lifecycle |
| **Main Window** | MainWindow.xaml | XAML UI definition with login and main panels |
| **Main Code-Behind** | MainWindow.xaml.cs | All UI event handlers, network communication orchestration |
| **TCP Client** | Networking/TcpClientService.cs | Manages TCP connection to server |

### UI Structure
- **Login Panel**: Server IP, Port, Username, Password fields + Register/Login buttons
- **Main Panel** (after login):
  - Header: User display, Disconnect button
  - Left: Upload controls, Secure Sharing controls, Receive Shared File controls
  - Right: Private files DataGrid, Download controls, Activity Logs list

### Key Client Flows
- Connect → Register/Login → Browse files → Upload in chunks → Download selected → Share files → Disconnect

## Server Side (FileTransfer.Server)

### Technology
- **WinForms (.NET Framework 4.7.2)**
- **Entity Framework Core 3.1.32** with PostgreSQL
- References: FileTransfer.Shared library

### Components

| Component | File | Responsibility |
|-----------|------|---------------|
| **Program Entry** | Program.cs | Application startup, runs Form1 |
| **Server UI** | Form1.cs | WinForms dashboard with controls, logs, status |
| **TCP Server** | Networking/TcpServer.cs | Listens for connections, routes messages to handlers |
| **Auth Service** | Services/AuthService.cs | Handles registration + login with BCrypt |
| **FileTransferState Service** | Services/FileTransferStateService.cs | Manages upload progress tracking (for resume) |
| **TransferHistory Service** | Services/TransferHistoryService.cs | Logs all file transfers to database |
| **SharedFile Service** | Services/SharedFileService.cs | Creates and validates share codes |
| **AdminCleanup Service** | Services/AdminCleanupService.cs | Clears transfer history logs |

### Server Message Router
TcpServer receives a NetworkMessage, deserializes it, then routes by MessageType:
```
NetworkMessage (Type + JsonBody)
        │
        ▼
MessageType switch:
├── Register       → HandleRegisterAsync()     → AuthService.RegisterAsync()
├── Login          → HandleLoginAsync()         → AuthService.LoginAsync()
├── FileStart      → HandleFileStart()          → Create empty file + save state
├── FileChunk      → HandleFileChunk()          → Decrypt + append to file
├── FileComplete   → HandleFileComplete()       → Mark complete + log history
├── GetFileList    → HandleGetFileList()        → List user's storage folder
├── DownloadFile   → HandleDownloadFile()       → Read file + return bytes
├── ResumeCheck    → HandleResumeCheck()        → Check FileTransferState
├── CreateShareCode→ HandleCreateShareCode()    → Generate share code
└── DownloadShared → HandleDownloadSharedFile() → Validate code + return file
```

## Shared Library (FileTransfer.Shared)

### Technology
- **.NET Framework 4.7.2 Class Library**

### Responsibilities
- Define the **protocol** (NetworkMessage, MessageType)
- Define all **DTOs** for request/response
- Provide **serialization helpers** (JSON)
- Provide **TCP message helpers** (length-prefix)
- Provide **AES encryption** utilities
- Define **base response** types

### Data Flow (Single Session)
```
Client                          Server
  │                               │
  │──── Connect (TCP)───────────► │
  │                               │
  │──── Register/Login JSON─────► │
  │◄─── BaseResponse ────────────│
  │                               │
  │──── FileStart JSON──────────► │ ← Creates empty file
  │◄─── BaseResponse ────────────│
  │                               │
  │──── FileChunk (encrypted)───► │ ← Decrypts + appends
  │◄─── BaseResponse ────────────│
  │       (repeat for N chunks)  │
  │                               │
  │──── FileComplete JSON───────► │ ← Marks complete
  │◄─── BaseResponse ────────────│
  │                               │
  │──── GetFileList JSON────────► │
  │◄─── FileListResponse ────────│
  │                               │
  │──── DownloadFile JSON───────► │
  │◄─── DownloadFileResponse─────│
  │                               │
  │──── Disconnect ─────────────►│
```

## Database Layer

**ORM**: Entity Framework Core 3.1.32
**Database Provider**: Npgsql (PostgreSQL)
**Hosting**: Render.com (Cloud PostgreSQL)
**Connection Security**: SSL Mode Require, Trust Server Certificate

### Entity Relationship Diagram (Text)
```
┌───────────────┐        ┌───────────────────┐
│     User      │        │   ClientSession    │
│───────────────│        │───────────────────│
│ Id (PK)       │──1:N──►│ Id (PK)            │
│ Username      │        │ UserId (FK)        │
│ PasswordHash  │        │ ClientIp           │
│ CreatedAt     │        │ ConnectedAt        │
└───────────────┘        │ DisconnectedAt     │
                         │ IsOnline           │
                         └───────────────────┘

┌─────────────────────┐  ┌───────────────────────┐
│  FileTransferState  │  │   TransferHistory      │
│─────────────────────│  │───────────────────────│
│ Id (PK)             │  │ Id (PK)                │
│ FileId              │  │ Username               │
│ FileName            │  │ FileName               │
│ TotalBytes          │  │ FileSize               │
│ BytesReceived       │  │ TransferType           │
│ LastChunkIndex      │  │ Status                 │
│ IsCompleted         │  │ CreatedAt              │
│ UpdatedAt           │  └───────────────────────┘
└─────────────────────┘

┌─────────────────────────┐
│      SharedFile          │
│─────────────────────────│
│ Id (PK)                  │
│ OwnerUsername            │
│ FileName                 │
│ ShareCode (unique)       │
│ AllowedUsername          │
│ CreatedAt                │
│ IsActive                 │
└─────────────────────────┘
```

## File Storage Architecture

```
/Storage/                    (Root storage - gitignored)
├── user1/                  (Per-user private folder)
│   ├── document.pdf
│   ├── image.jpg
│   └── ...
├── user2/
│   ├── report.docx
│   └── ...
└── ...
```

- Each logged-in user has a private subdirectory named after their username
- Files are stored in plaintext on server (encryption applies only during transit)
- Storage path is determined by `GetUserStorageFolder(username)`
- Root storage is located relative to the server executable

## Project Solution Structure

```
FileTransferSolution.sln
├── FileTransfer.Client (WPF) → references FileTransfer.Shared
├── FileTransfer.Server (WinForms) → references FileTransfer.Shared
└── FileTransfer.Shared (Class Library)
```

All three projects target .NET Framework 4.7.2.
The solution is built with Visual Studio 2019 (Version 16).