# Features Documentation

## Feature List

### 1. TCP Connection to Server

**Purpose**: Establish a persistent TCP socket connection between the WPF client and the WinForms server for real-time communication.

**User Flow**:
1. User enters Server IP (e.g., 127.0.0.1 or LAN IP) and Port (default 9000)
2. User clicks "Connect to Server"
3. Client calls `TcpClientService.ConnectAsync(ip, port)`
4. Server accepts the connection via `TcpListener.AcceptTcpClientAsync()`
5. UI updates to "Status: Connected"
6. Register and Login buttons become enabled

**Main Classes**:
- `MainWindow` (Client) - `btnConnect_Click` handler
- `TcpClientService` (Client) - Manages TcpClient + NetworkStream
- `TcpServer` (Server) - Listens and accepts connections

**Database Tables**: None (connection is in-memory)

---

### 2. User Registration

**Purpose**: Allow new users to create an account with a username and password.

**User Flow**:
1. User enters Username and Password
2. User clicks "Register"
3. Client creates `RegisterRequestDto` {Username, Password}
4. Client wraps it in `NetworkMessage {Type=Register, JsonBody=serialized dto}`
5. Client sends JSON over TCP
6. Server deserializes and calls `AuthService.RegisterAsync()`
7. Server checks if username already exists in DB
8. If not, BCrypt hashes the password
9. Server saves `User` entity to PostgreSQL
10. Server returns `BaseResponseDto {Success=true, Message="Đăng ký thành công"}`
11. Client shows MessageBox with response message

**Main Classes**:
- `MainWindow` (Client) - `btnRegister_Click`
- `TcpServer` (Server) - `HandleRegisterAsync()`
- `AuthService` (Server) - `RegisterAsync()`
- `AppDbContext` (Server) - Database access

**Database Tables**: `Users`

---

### 3. User Login

**Purpose**: Authenticate an existing user and establish a session.

**User Flow**:
1. User enters Username and Password
2. User clicks "Login"
3. Client creates `LoginRequestDto` {Username, Password}
4. Client sends via `MessageType.Login`
5. Server calls `AuthService.LoginAsync()`
6. Server looks up user by username in DB
7. Server verifies password using `BCrypt.Verify(password, hash)`
8. If valid:
   - Creates `ClientSession` record (UserId, ClientIp, IsOnline=true)
   - Maps TcpClient to username in `_clientUsers` dictionary
   - Returns "Đăng nhập thành công"
9. If invalid:
   - Returns "Tài khoản không tồn tại" or "Sai mật khẩu"
10. On success: login panel hides, main panel shows, user name displayed

**Main Classes**:
- `MainWindow` (Client) - `btnLogin_Click`
- `TcpServer` (Server) - `HandleLoginAsync()`
- `AuthService` (Server) - `LoginAsync()`
- `ClientSession` entity

**Database Tables**: `Users`, `ClientSessions`

---

### 4. Browse Files (Multi-select)

**Purpose**: Allow user to select one or multiple files from their local machine for upload.

**User Flow**:
1. User clicks "Choose Files"
2. `OpenFileDialog` opens with `Multiselect = true`
3. User selects files
4. File paths stored in `_selectedFiles` array
5. File names displayed in textbox (semicolon-separated)
6. Upload button enabled

**Main Classes**: `MainWindow` (Client) - `btnBrowse_Click`

**Database Tables**: None

---

### 5. File Upload with Chunking

**Purpose**: Upload files to the server by splitting them into encrypted 64 KB chunks for efficient transfer.

**User Flow**:
1. User selects files and clicks "Upload Selected Files"
2. For each file:
   a. Client generates a stable FileId using MD5 hash of (filename + size + last write time)
   b. Client sends `ResumeCheckRequestDto` to check if partial upload exists
   c. If file already completed, skip (100%)
   d. If partial upload exists, resume from last chunk
   e. If new upload, send `FileStartRequestDto`
   f. Server creates empty file and saves state `FileTransferState`
   g. Client reads file in 64KB chunks
   h. Each chunk is encrypted with `AesEncryptionHelper.Encrypt()`
   i. Encrypted chunk sent as `FileChunkDto`
   j. Server decrypts and appends to file
   k. Progress bar updates after each chunk
   l. After all chunks, send `FileCompleteDto`
   m. Server marks transfer complete and logs to `TransferHistory`

**Main Classes**:
- `MainWindow` - `btnUpload_Click`, `UploadSingleFileAsync()`
- `TcpServer` - `HandleFileStart()`, `HandleFileChunk()`, `HandleFileComplete()`
- `FileTransferStateService` - `SaveProgressAsync()`, `GetByFileId()`
- `TransferHistoryService` - `SaveAsync()`
- `AesEncryptionHelper` - `Encrypt()`, `Decrypt()`

**Database Tables**: `FileTransferStates`, `TransferHistories`

---

### 6. Resume Upload

**Purpose**: Automatically resume interrupted file uploads from where they left off.

**User Flow**:
1. Before uploading, client sends `ResumeCheckRequest` with FileId
2. Server looks up `FileTransferState` by FileId
3. If state found with `IsCompleted=false` and `BytesReceived > 0`:
   - Server returns `LastChunkIndex + 1` and `BytesReceived`
   - Client seeks file to `startBytePosition`
   - Client resumes sending subsequent chunks
4. If state found with `IsCompleted=true`:
   - Client skips file (already uploaded)
5. If no state found:
   - Client starts new upload from beginning

**Main Classes**:
- `MainWindow` - `SendResumeCheckAsync()`
- `TcpServer` - `HandleResumeCheck()`
- `FileTransferStateService` - `GetByFileId()`

**Database Tables**: `FileTransferStates`

---

### 7. Refresh File List

**Purpose**: Display all files in the user's private server storage.

**User Flow**:
1. User clicks "Refresh" button (or auto-refreshes after upload)
2. Client sends `GetFileList` message with empty body
3. Server calls `HandleGetFileList()`
4. Server reads all files from user's storage folder
5. Server returns `FileListResponseDto` with `List<FileInfoDto>`
6. Client binds list to `DataGrid` showing FileName, FileSize, UploadedAt

**Main Classes**:
- `MainWindow` - `btnRefreshFiles_Click`, `RefreshFileListAsync()`
- `TcpServer` - `HandleGetFileList()`
- `FileInfoDto`

**Database Tables**: None (reads from file system)

---

### 8. Download File

**Purpose**: Download a selected file from the server to the local machine.

**User Flow**:
1. User selects a file from the DataGrid
2. User clicks "Download Selected"
3. `SaveFileDialog` opens with the file name pre-filled
4. User chooses save location
5. Client sends `DownloadFileRequestDto` {FileName}
6. Server reads file bytes from user's storage folder
7. Server logs to `TransferHistory` (type: "Download")
8. Server returns `DownloadFileResponseDto` {FileName, FileData (byte[])}
9. Client writes bytes to local file
10. Progress bar shows 100%

**Main Classes**:
- `MainWindow` - `btnDownload_Click`, `SendDownloadRequestAsync()`
- `TcpServer` - `HandleDownloadFile()`
- `TransferHistoryService` - `SaveAsync()`

**Database Tables**: `TransferHistories`

---

### 9. Create Share Code

**Purpose**: Generate a unique share code to grant another user access to download a specific file from your private storage.

**User Flow**:
1. User selects a file from the DataGrid
2. User enters the recipient's username in "Allowed username" field
3. User clicks "Create Share Code"
4. Client sends `CreateShareCodeRequestDto` {FileName, AllowedUsername}
5. Server validates file exists in owner's storage
6. `SharedFileService.CreateShareCodeAsync()` generates an 8-character uppercase GUID
7. Server saves `SharedFile` entity to DB (owner, filename, code, allowed user, active=true)
8. Server returns `CreateShareCodeResponseDto` {ShareCode}
9. Client displays the generated code
10. Owner shares this code with the recipient (out-of-band)

**Main Classes**:
- `MainWindow` - `btnCreateShareCode_Click`, `SendCreateShareCodeAsync()`
- `TcpServer` - `HandleCreateShareCode()`
- `SharedFileService` - `CreateShareCodeAsync()`

**Database Tables**: `SharedFiles`

---

### 10. Download Shared File

**Purpose**: Download a file shared by another user using a share code.

**User Flow**:
1. Recipient enters the share code in "Share code" field
2. Recipient clicks "Download Shared File"
3. Client sends `DownloadSharedFileRequestDto` {ShareCode}
4. Server looks up `SharedFile` by code where `IsActive=true`
5. Server validates the requesting user matches `AllowedUsername`
6. Server reads file from the owner's storage folder
7. Server logs to `TransferHistory` (type: "DownloadShared")
8. Server returns `DownloadFileResponseDto` {FileName, FileData}
9. Recipient saves the file locally

**Main Classes**:
- `MainWindow` - `btnDownloadSharedFile_Click`, `SendDownloadSharedFileAsync()`
- `TcpServer` - `HandleDownloadSharedFile()`
- `SharedFileService` - `GetByShareCode()`

**Database Tables**: `SharedFiles`, `TransferHistories`

---

### 11. Disconnect

**Purpose**: Gracefully disconnect from the server and return to login screen.

**User Flow**:
1. User clicks "Disconnect"
2. Client calls `TcpClientService.Disconnect()`
3. Server detects connection close in `HandleClientAsync()`
4. Server removes client from `_clientUsers` dictionary
5. Client UI switches back to login panel
6. All buttons return to disconnected state

**Main Classes**:
- `MainWindow` - `btnDisconnect_Click`
- `TcpClientService` - `Disconnect()`
- `TcpServer` - `HandleClientAsync()` (finally block)

**Database Tables**: None (in-memory session tracking)

---

### 12. Server Dashboard (Admin)

**Purpose**: Provide server administrators with a real-time management interface.

**User Flow**:
1. Administrator launches the WinForms server application
2. Dashboard shows:
   - Server status (RUNNING/STOPPED)
   - IP address and port
   - Storage path
   - Database status (PostgreSQL Render)
   - AES encryption indicator
   - Chunk size (64 KB)
   - Started at time
   - Uptime counter
3. Administrator can:
   - Start server (listens on configured port)
   - Stop server
   - Restart server
   - Open Storage folder in File Explorer
   - Clear UI logs
   - Clear database transfer logs (with confirmation)

**Main Classes**:
- `Form1` - BuildModernUi(), event handlers
- `TcpServer` - StartAsync(), Stop()
- `AdminCleanupService` - ClearLogsAsync()

**Database Tables**: `TransferHistories` (cleared via admin action)

---

### 13. Activity Logging

**Purpose**: Display real-time activity logs on both client and server UIs.

**User Flow**:
1. Client: Every network operation adds a timestamped log entry to `lstLogs` ListBox
2. Server: Every connection, disconnection, upload, download, share code creation is logged
3. Logs include: HH:mm:ss - [message]
4. Server logs are color-coded via console look on dark UI
5. Auto-scroll to latest log entry

**Main Classes**:
- `MainWindow` - `AddLog()`
- `Form1` - `AddLog()` (thread-safe with Invoke)
- `TcpServer` - `OnLog` event

**Database Tables**: None (UI-only)

---

### 14. Transfer History Persistence

**Purpose**: Record all file transfers for auditing and tracking.

**User Flow**:
1. Every completed upload, download, and shared download triggers `TransferHistoryService.SaveAsync()`
2. Records: username, filename, file size, transfer type (Upload/Download/DownloadShared), status (Success), timestamp
3. Admin can clear all history from the dashboard

**Main Classes**:
- `TransferHistoryService`
- `TcpServer` - calls `SaveAsync()` after FileComplete and DownloadFile handlers

**Database Tables**: `TransferHistories`

---

### 15. Unique File ID Generation

**Purpose**: Generate a consistent identifier for each file to support resume upload functionality.

**How it works**:
1. Client creates MD5 hash of: `FileName | FileLength | LastWriteTimeUtc.Ticks`
2. Result is a 32-character hex string (e.g., "a1b2c3d4e5f6...")
3. This FileId is unique per file per version
4. Same file with same timestamp yields same FileId (intentional for resume)

**Main Classes**:
- `MainWindow` - `CreateStableFileId()`

**Database Tables**: `FileTransferStates` (stores FileId)