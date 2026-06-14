# Project Overview: Secure File Transfer Client-Server

## Project Purpose

The Secure File Transfer Client-Server is a real-time file transfer application built using a traditional client-server architecture. The software allows users to securely upload, download, and share files between a WPF client and a WinForms server over a TCP socket network connection. All file transfers are encrypted, and user authentication is handled with password hashing.

## Problem Solved

This project solves the need for a private, encrypted, and resumable file transfer system where:

- Users can upload files of any size to a remote server
- Users can download their stored files on demand  
- Users can share files with other specific users via secure share codes
- Transfers can be resumed if interrupted (e.g., network failure)
- Multiple files can be uploaded simultaneously
- Each user has an isolated private storage directory

## Main Features

### Core Features
- **User Registration & Login**: Users create an account with username/password
- **File Upload**: Upload files split into encrypted chunks (64 KB each)
- **File Download**: Download stored files from personal storage
- **Resume Upload**: Automatically resume interrupted file uploads
- **Multi-file Upload**: Select and upload multiple files at once
- **Real-time Progress**: UI progress bars and percentage tracking
- **File Sharing**: Generate share codes to share files with specific users
- **Transfer History**: All upload/download activity is logged in database

### Security Features
- **BCrypt Password Hashing**: Passwords are never stored in plaintext
- **AES-256 File Encryption**: Each file chunk is encrypted before transmission
- **Private User Storage**: Each user's files are isolated in their own folder
- **Share Code Access Control**: Shared files can be restricted to specific usernames

### Server Dashboard
- WinForms UI showing server status, IP/port, uptime
- Real-time activity logs
- Start/Stop/Restart controls
- Storage folder quick access
- Database log clearing

## User Roles

1. **End User (Client)**: Connects via WPF application. Can register, login, upload, download, and share files.
2. **Server Administrator**: Manages the WinForms server application. Can start/stop the server, monitor activity, and manage storage.

## Main Workflows

1. **Connect** → Enter Server IP and Port → Click Connect
2. **Register** → Enter Username + Password → Click Register
3. **Login** → Enter credentials → Click Login → Main screen appears
4. **Upload** → Browse files → Select one or more → Click Upload → Progress shown
5. **Download** → Select file from list → Click Download → Choose save location
6. **Share** → Select file → Enter target username → Click Create Share Code → Send code to recipient
7. **Receive Shared File** → Enter share code → Click Download Shared File
8. **Disconnect** → Click Disconnect → Return to login screen

## Technology Stack

| Layer | Technology |
|-------|-----------|
| **Client UI** | WPF (.NET Framework 4.7.2) |
| **Server UI** | WinForms (.NET Framework 4.7.2) |
| **Shared Library** | .NET Framework 4.7.2 Class Library |
| **Transport** | TCP Sockets (Length-prefix protocol) |
| **Serialization** | JSON (Newtonsoft.Json) |
| **Database ORM** | Entity Framework Core 3.1.32 |
| **Database** | PostgreSQL (hosted on Render.com) |
| **Password Hashing** | BCrypt.Net-Next 4.2.0 |
| **File Encryption** | AES (via .NET System.Security.Cryptography) |

## Architecture

```
┌─────────────────────┐     TCP Socket      ┌──────────────────────┐
│                     │    (JSON messages)   │                      │
│   WPF Client        │ ◄──────────────────► │   WinForms Server    │
│   (FileTransfer     │     Length-prefix    │   (FileTransfer      │
│    .Client)         │     protocol         │    .Server)          │
│                     │                      │                      │
│  - MainWindow.xaml  │                      │  - Form1.cs (UI)     │
│  - TcpClientService  │                     │  - TcpServer.cs      │
│                     │                      │  - AuthService       │
│                     │                      │  - Services layer    │
└─────────────────────┘                      └──────────┬───────────┘
                                                        │
                                                        │ EF Core
                                                        ▼
                                              ┌─────────────────────┐
                                              │                     │
                                              │   PostgreSQL        │
                                              │   (Render Cloud)    │
                                              │                     │
                                              │ - Users             │
                                              │ - ClientSessions    │
                                              │ - TransferHistories │
                                              │ - FileTransferStates│
                                              │ - SharedFiles       │
                                              └─────────────────────┘
```

## Development Status

The project appears to be **production-ready / completed** with:
- Fully functional client-server communication
- Database migrations applied
- Cloud-hosted PostgreSQL database
- All major features implemented
- Modern UI design on both client and server

## Project Structure

```
FileTransferSolution.sln
├── FileTransfer.Client      (WPF Application)
│   ├── App.xaml / App.xaml.cs
│   ├── MainWindow.xaml / MainWindow.xaml.cs
│   └── Networking/
│       └── TcpClientService.cs
├── FileTransfer.Server      (WinForms Application)
│   ├── Program.cs
│   ├── Form1.cs / Form1.Designer.cs
│   ├── Database/
│   │   └── AppDbContext.cs
│   ├── Entities/
│   │   ├── User.cs
│   │   ├── ClientSession.cs
│   │   ├── FileTransferState.cs
│   │   ├── TransferHistory.cs
│   │   └── SharedFile.cs
│   ├── Migrations/
│   │   ├── InitialPostgresCreate
│   │   ├── AddClientSessions
│   │   ├── AddTransferHistories
│   │   ├── AddFileTransferStates
│   │   └── AddSharedFiles
│   ├── Networking/
│   │   └── TcpServer.cs
│   ├── Services/
│   │   ├── AuthService.cs
│   │   ├── FileTransferStateService.cs
│   │   ├── TransferHistoryService.cs
│   │   ├── SharedFileService.cs
│   │   └── AdminCleanupService.cs
│   └── Storage/             (User file storage - gitignored)
└── FileTransfer.Shared      (Class Library)
    ├── DTOs/
    │   ├── RegisterRequestDto.cs
    │   ├── LoginRequestDto.cs
    │   ├── FileStartRequestDto.cs
    │   ├── FileChunkDto.cs
    │   ├── FileCompleteDto.cs
    │   ├── FileInfoDto.cs
    │   ├── ResumeCheckRequestDto.cs
    │   ├── ResumeCheckResponseDto.cs
    │   ├── DownloadFileRequestDto.cs
    │   ├── DownloadFileResponseDto.cs
    │   ├── CreateShareCodeRequestDto.cs
    │   ├── CreateShareCodeResponseDto.cs
    │   └── DownloadSharedFileRequestDto.cs
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