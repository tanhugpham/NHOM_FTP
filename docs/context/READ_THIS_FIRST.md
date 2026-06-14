# READ THIS FIRST - Secure File Transfer Client-Server

## What Is This Project?

This is a **file transfer application** that lets users upload, download, and share files between a desktop client and a server over a local network (LAN) or the internet. Think of it as a **private, self-hosted cloud storage** system with encryption.

## The Big Picture

```
WPF Client (User) ──TCP Socket──► WinForms Server ──EF Core──► PostgreSQL
```

- **Users** connect to the server using a WPF desktop application
- **Server** runs on a Windows machine with a dashboard UI for administrators
- **Database** is hosted in the cloud (PostgreSQL on Render.com)
- **Communication** is over raw TCP sockets with JSON messages

## Three Main Projects

### 1. FileTransfer.Client (WPF Application)
**What the user sees and interacts with.**

- **MainWindow.xaml** - The complete UI (login screen + main dashboard)
- **MainWindow.xaml.cs** - All the code that makes buttons work (connect, login, upload, download, share, disconnect)
- **TcpClientService.cs** - Manages the TCP connection to the server

**What users can do:**
- Connect to a server by IP address and port
- Register a new account
- Log in
- Upload files (any size, split into encrypted 64KB chunks)
- Resume interrupted uploads automatically
- Download files from their private storage
- Share files with other users using share codes
- Download files shared by others

### 2. FileTransfer.Server (WinForms Application)
**The brain of the operation.**

- **TcpServer.cs** - The core: listens for connections, accepts clients, routes messages to handlers
- **Form1.cs** - Server dashboard (dark theme UI with status, logs, start/stop controls)
- **Services/** - Business logic layer:
  - `AuthService.cs` - Registration and login with BCrypt password hashing
  - `FileTransferStateService.cs` - Tracks upload progress for resume
  - `TransferHistoryService.cs` - Logs all file transfers
  - `SharedFileService.cs` - Creates and validates share codes
  - `AdminCleanupService.cs` - Clears transfer logs
- **Entities/** - Database models (User, ClientSession, FileTransferState, TransferHistory, SharedFile)
- **Database/AppDbContext.cs** - EF Core database context (PostgreSQL)

### 3. FileTransfer.Shared (Class Library)
**The shared vocabulary between client and server.**

- **NetworkMessage** - The envelope: contains MessageType + JSON body
- **MessageType** - Enum defining all possible message types (Register, Login, FileStart, FileChunk, etc.)
- **DTOs/** - Data Transfer Objects for every request/response (like data contracts)
- **Helpers/** - JSON serialization (JsonHelper) and TCP framing (TcpMessageHelper)
- **Security/AesEncryptionHelper.cs** - AES-256 encryption for file chunks
- **Responses/** - BaseResponseDto and specialized response types

## Most Important Modules

### 1. Networking (TCP Protocol)
```
[4-byte length] [UTF-8 JSON message]
```
Every message has a 4-byte length prefix followed by a JSON string. This solves the TCP "message boundary" problem. Client sends a request, server processes it, server sends back exactly one response.

### 2. File Upload System
Files are uploaded in 64KB chunks. Each chunk is:
1. Read from the local file
2. Encrypted with AES-256
3. Sent as a JSON message
4. Decrypted by the server
5. Appended to the file on disk

**Resume support**: If the upload is interrupted, the server remembers how much was received. When the client reconnects, it checks and resumes from where it left off.

### 3. Authentication
- Passwords are hashed with **BCrypt** before storage (never plaintext)
- On login, the server verifies the hash and creates a session record
- The server maps each TCP socket to the authenticated username

### 4. File Sharing
- File owners generate an **8-character share code**
- Codes are restricted to specific recipient usernames
- Recipients enter the code to download the shared file

## How Everything Connects

```
User clicks "Upload" in WPF Client
        │
        ▼
MainWindow creates FileChunkDto, encrypts data
        │
        ▼
TcpClientService sends JSON over TCP
        │
        ▼
TcpServer receives JSON, routes to HandleFileChunk
        │
        ▼
Server decrypts data, appends to file on disk
        │
        ▼
Server updates FileTransferState in PostgreSQL
        │
        ▼
Server sends response back to client
        │
        ▼
Client updates progress bar
```

## Key Technical Details

| Aspect | Detail |
|--------|--------|
| **Framework** | .NET Framework 4.7.2 |
| **Client UI** | WPF (XAML + code-behind) |
| **Server UI** | WinForms (Form) |
| **Database** | PostgreSQL (Render.com cloud) |
| **ORM** | Entity Framework Core 3.1.32 |
| **Password Hashing** | BCrypt.Net-Next 4.2.0 |
| **File Encryption** | AES-256 (hardcoded key/IV) |
| **Serialization** | JSON (Newtonsoft.Json) |
| **Chunk Size** | 64 KB |
| **Default Port** | 9000 |

## Security Summary (Important)

**What's good:**
- Passwords are BCrypt hashed at rest
- File chunks are AES encrypted in transit
- Each user has isolated private storage
- Path traversal is prevented

**What's weak:**
- Passwords are sent in plaintext over TCP (no TLS)
- AES keys are hardcoded in source code
- No rate limiting on login
- Sessions are never properly closed
- Share codes never expire

## Quick Start (How to Run)

1. Open `FileTransferSolution.sln` in Visual Studio 2019+
2. Build the entire solution (Ctrl+Shift+B)
3. Run **FileTransfer.Server** → Click "Start Server"
4. Run **FileTransfer.Client** → Enter Server IP:Port → Click Connect
5. Register a new account → Login
6. Upload files, download files, share files

## What Problem Does This Solve?

This project solves the need for a **private, secure file transfer system** where:
- You control the server (or your organization does)
- Files are encrypted during transfer
- Uploads survive network interruptions
- You can securely share files with specific people
- Everything is logged for audit

---

**Start with these files for deep dives:**
- `ARCHITECTURE.md` - Full system architecture with diagrams
- `FEATURES.md` - Every feature explained in detail
- `NETWORKING.md` - How TCP communication works
- `SECURITY.md` - All security mechanisms and weaknesses
- `DATABASE.md` - Table structures and relationships
- `CLASS_MAP.md` - Every class with its responsibility
- `IMPORTANT_FLOWS.md` - Step-by-step flow diagrams
- `DEFENSE_GUIDE.md` - Common questions and answers for presentations