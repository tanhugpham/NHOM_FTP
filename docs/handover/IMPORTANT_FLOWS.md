# Important Flows

## Flow 1: Server Startup

```
Server Administrator
        │
        ▼
┌───────────────────────────────┐
│  Launch FileTransfer.Server   │
│  (WinForms application)       │
└───────────────────────────────┘
        │
        ▼
┌───────────────────────────────┐
│  Form1 Constructor            │
│  - BuildModernUi()            │
│  - Initialize TcpServer       │
│  - Set up timer for uptime    │
│  - Default port: 9000         │
└───────────────────────────────┘
        │
        ▼
┌───────────────────────────────┐
│  Admin clicks "Start Server"  │
│  (btnStart_Click)             │
└───────────────────────────────┘
        │
        ▼
┌───────────────────────────────┐
│  TcpServer.StartAsync(9000)   │
│  - Create TcpListener         │
│  - Listen on IPAddress.Any    │
│  - Accept clients in loop     │
│  - Fire-and-forget each       │
│    client handler             │
└───────────────────────────────┘
        │
        │  UI Update:
        ▼
┌───────────────────────────────┐
│  Status: RUNNING (green)      │
│  Started At: HH:mm:ss         │
│  Uptime: 00:00:01 (increment) │
│  Footer: "Server running"     │
└───────────────────────────────┘
```

**Key Classes**: `Form1`, `TcpServer`  
**Database**: None (pure TCP)

---

## Flow 2: Client Connection and Login

```
User (Client)                       Server                       Database
     │                                │                             │
     │  Enter IP: 127.0.0.1           │                             │
     │  Enter Port: 9000              │                             │
     │  Click "Connect to Server"     │                             │
     │───────────────────────────────►│                             │
     │  TcpClient.ConnectAsync()      │                             │
     │                                │  TcpListener.AcceptClient() │
     │                                │  _=HandleClientAsync()      │
     │  Status: Connected             │                             │
     │◄───────────────────────────────│                             │
     │                                │                             │
     │  Enter Username + Password     │                             │
     │  Click "Login"                 │                             │
     │───────────────────────────────►│                             │
     │  MessageType.Login             │                             │
     │  LoginRequestDto {Username,    │                             │
     │    Password}                   │                             │
     │                                │  AuthService.LoginAsync()   │
     │                                │──────────────────────────►  │
     │                                │  SELECT * FROM Users        │
     │                                │  WHERE Username = @u        │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │                                │  BCrypt.Verify(password,    │
     │                                │    PasswordHash)            │
     │                                │                             │
     │                                │  INSERT INTO ClientSessions │
     │                                │  (UserId, ClientIp,         │
     │                                │   IsOnline=true)            │
     │                                │──────────────────────────►  │
     │                                │                             │
     │                                │  _clientUsers[client] = u   │
     │                                │                             │
     │  MessageBox("Đăng nhập        │                             │
     │    thành công")                │                             │
     │◄───────────────────────────────│                             │
     │                                │                             │
     │  LoginPanel → Hidden           │                             │
     │  MainPanel → Visible           │                             │
     │  User: {username} displayed    │                             │
```

**Key Classes**: `MainWindow`, `TcpClientService`, `TcpServer`, `AuthService`, `AppDbContext`  
**Database Tables**: `Users`, `ClientSessions`

---

## Flow 3: File Upload (With Resume)

```
User (Client)                       Server                       Database
     │                                │                             │
     │  Click "Choose Files"          │                             │
     │  OpenFileDialog (multiselect)  │                             │
     │  Select files                  │                             │
     │                                │                             │
     │  Click "Upload Selected Files" │                             │
     │──────────────────────────────────────────────────────────────│
     │  FOR EACH FILE:                │                             │
     │                                │                             │
     │  Generate FileId =             │                             │
     │    MD5(Name|Size|LastWrite)    │                             │
     │                                │                             │
     │  ResumeCheckRequest {FileId}──►│                             │
     │                                │  FileTransferStateService   │
     │                                │  .GetByFileId(FileId)       │
     │                                │──────────────────────────►  │
     │                                │  SELECT * FROM              │
     │                                │  FileTransferStates         │
     │                                │  WHERE FileId = @fid        │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │◄── ResumeCheckResponse {       │                             │
     │      LastChunkIndex,           │                             │
     │      BytesReceived,            │                             │
     │      IsCompleted}              │                             │
     │                                │                             │
     │  IF IsCompleted → SKIP (100%)  │                             │
     │                                │                             │
     │  IF BytesReceived > 0 → RESUME │                             │
     │  ELSE → NEW UPLOAD             │                             │
     │                                │                             │
     │  FileStartRequest {FileId,     │                             │
     │    FileName, TotalBytes}──────►│                             │
     │                                │  Create empty file on disk  │
     │                                │  SaveProgressAsync()        │
     │                                │──────────────────────────►  │
     │                                │  INSERT/UPDATE              │
     │                                │  FileTransferStates         │
     │                                │◄──────────────────────────  │
     │◄── BaseResponse {Success=true} │                             │
     │                                │                             │
     │  [LOOP] Read 64KB chunk        │                             │
     │  Encrypt chunk (AES)           │                             │
     │                                │                             │
     │  FileChunk {FileId,            │                             │
     │    EncryptedData, ChunkIndex,  │                             │
     │    IsLastChunk} ──────────────►│                             │
     │                                │  AesEncryptionHelper        │
     │                                │  .Decrypt(chunkData)        │
     │                                │  Append to file on disk     │
     │                                │  SaveProgressAsync()        │
     │                                │──────────────────────────►  │
     │                                │  UPDATE FileTransferStates  │
     │                                │◄──────────────────────────  │
     │◄── BaseResponse {Success=true} │                             │
     │                                │                             │
     │  Update ProgressBar            │                             │
     │  Update Log                    │                             │
     │                                │                             │
     │  [END LOOP - last chunk]       │                             │
     │                                │                             │
     │  FileComplete {FileId,Name}───►│                             │
     │                                │  SaveProgressAsync(         │
     │                                │    IsCompleted=true)        │
     │                                │──────────────────────────►  │
     │                                │  UPDATE FileTransferStates  │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │                                │  TransferHistoryService     │
     │                                │  .SaveAsync(Upload,Success) │
     │                                │──────────────────────────►  │
     │                                │  INSERT TransferHistories   │
     │                                │◄──────────────────────────  │
     │◄── BaseResponse {Success=true} │                             │
     │                                │                             │
     │  ProgressBar = 100%            │                             │
     │                                │                             │
     │  [NEXT FILE]                   │                             │
     │                                │                             │
     │  RefreshFileListAsync()        │                             │
     │───────────────────────────────►│                             │
     │◄── FileListResponse {Files[]}  │                             │
     │                                │                             │
     │  MessageBox("Upload complete") │                             │
```

**Key Classes**: `MainWindow`, `TcpServer`, `FileTransferStateService`, `TransferHistoryService`, `AesEncryptionHelper`  
**Database Tables**: `FileTransferStates`, `TransferHistories`

---

## Flow 4: File Download

```
User (Client)                       Server                       Database
     │                                │                             │
     │  View file list (DataGrid)     │                             │
     │  Select a file                 │                             │
     │  Click "Download Selected"     │                             │
     │                                │                             │
     │  SaveFileDialog opens          │                             │
     │  (pre-filled with filename)    │                             │
     │  User chooses location         │                             │
     │                                │                             │
     │  DownloadFileRequest {         │                             │
     │    FileName} ─────────────────►│                             │
     │                                │  Validate user owns file    │
     │                                │  Read file bytes from disk  │
     │                                │                             │
     │                                │  TransferHistoryService     │
     │                                │  .SaveAsync(Download,       │
     │                                │    Success)                 │
     │                                │──────────────────────────►  │
     │                                │  INSERT TransferHistories   │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │◄── DownloadFileResponse {      │                             │
     │      FileName,                 │                             │
     │      FileData (byte[])}        │                             │
     │                                │                             │
     │  File.WriteAllBytes(savePath,  │                             │
     │    FileData)                   │                             │
     │                                │                             │
     │  ProgressBar = 100%            │                             │
     │  Log: "Downloaded: {file}"     │                             │
     │  MessageBox("Download thành    │                             │
     │    công")                      │                             │
```

**Key Classes**: `MainWindow`, `TcpServer`, `TransferHistoryService`  
**Database Tables**: `TransferHistories`

---

## Flow 5: File Sharing (Create Share Code)

```
Owner (Client)                      Server                       Database
     │                                │                             │
     │  Select a file in DataGrid     │                             │
     │  Enter recipient username      │                             │
     │  Click "Create Share Code"     │                             │
     │                                │                             │
     │  CreateShareCodeRequest {      │                             │
     │    FileName,                   │                             │
     │    AllowedUsername} ──────────►│                             │
     │                                │  Validate file exists       │
     │                                │  in owner's storage         │
     │                                │                             │
     │                                │  ShareCode =                │
     │                                │    Guid(8 chars).ToUpper()  │
     │                                │                             │
     │                                │  INSERT SharedFile          │
     │                                │  (OwnerUsername, FileName,  │
     │                                │   ShareCode, AllowedUser,   │
     │                                │   IsActive=true)            │
     │                                │──────────────────────────►  │
     │                                │                             │
     │◄── CreateShareCodeResponse {   │                             │
     │      ShareCode: "A1B2C3D4"}    │                             │
     │                                │                             │
     │  Display code in UI            │                             │
     │  MessageBox with code          │                             │
     │                                │                             │
     │  Owner sends code via          │                             │
     │  external channel (chat/email) │                             │
```

**Key Classes**: `MainWindow`, `TcpServer`, `SharedFileService`  
**Database Tables**: `SharedFiles`

---

## Flow 6: Download Shared File

```
Recipient (Client)                  Server                       Database
     │                                │                             │
     │  Enter share code               │                             │
     │  Click "Download Shared File"   │                             │
     │                                │                             │
     │  DownloadSharedFileRequest {   │                             │
     │    ShareCode: "A1B2C3D4"} ────►│                             │
     │                                │                             │
     │                                │  SharedFileService          │
     │                                │  .GetByShareCode(code)      │
     │                                │──────────────────────────►  │
     │                                │  SELECT * FROM SharedFiles  │
     │                                │  WHERE ShareCode=@c         │
     │                                │    AND IsActive=true        │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │                                │  IF not found:              │
     │                                │    "Mã chia sẻ không hợp lệ"│
     │                                │                             │
     │                                │  IF AllowedUsername !=       │
     │                                │     currentUser:            │
     │                                │    "Bạn không có quyền tải" │
     │                                │                             │
     │                                │  Read file from owner's     │
     │                                │  storage folder             │
     │                                │                             │
     │                                │  TransferHistoryService     │
     │                                │  .SaveAsync(DownloadShared, │
     │                                │    Success)                 │
     │                                │──────────────────────────►  │
     │                                │  INSERT TransferHistories   │
     │                                │◄──────────────────────────  │
     │                                │                             │
     │◄── DownloadFileResponse {      │                             │
     │      FileName, FileData}       │                             │
     │                                │                             │
     │  SaveFileDialog → Save file    │                             │
     │  MessageBox("Download file     │                             │
     │    chia sẻ thành công")        │                             │
```

**Key Classes**: `MainWindow`, `TcpServer`, `SharedFileService`, `TransferHistoryService`  
**Database Tables**: `SharedFiles`, `TransferHistories`

---

## Flow 7: Disconnect

```
User (Client)                       Server
     │                                │
     │  Click "Disconnect"            │
     │───────────────────────────────►│
     │                                │
     │  TcpClientService.Disconnect() │
     │  - _stream.Close()             │
     │  - _client.Close()             │
     │                                │
     │                                │  Server detects TCP close    │
     │                                │  HandleClientAsync() finally │
     │                                │  - Remove from _clientUsers │
     │                                │  - client.Close()            │
     │                                │  - Log: "Client disconnected"│
     │                                │
     │  UI:                           │
     │  - LoginPanel visible          │
     │  - MainPanel collapsed         │
     │  - Status: "Disconnected"      │
     │  - All buttons disabled        │
     │  - User: "-"                   │
```

**Key Classes**: `MainWindow`, `TcpClientService`, `TcpServer`  
**Database**: None (in-memory dictionary cleanup only)

---

## Flow 8: Server Shutdown

```
Admin clicks "Stop Server"
        │
        ▼
┌───────────────────────────────┐
│  btnStop_Click()              │
│  _server.Stop()               │
└───────────────────────────────┘
        │
        ▼
┌───────────────────────────────┐
│  TcpServer.Stop()             │
│  - _cts.Cancel()              │
│    (stops accept loop)        │
│  - _listener.Stop()           │
│  - IsRunning = false          │
│  - OnLog("Server stopped")    │
└───────────────────────────────┘
        │
        │  Note: Existing client
        │  connections are NOT
        │  closed by Stop()
        ▼
┌───────────────────────────────┐
│  UI Update:                   │
│  Status: STOPPED (red)        │
│  Footer: "Server stopped"     │
│  Start button re-enabled      │
└───────────────────────────────┘
```

**Key Classes**: `Form1`, `TcpServer`  
**Database**: None

---

## Flow 9: Registration Error - Username Already Exists

```
User (Client)                       Server                       Database
     │                                │                             │
     │  Enter username: "john"        │                             │
     │  Enter password: "secret"      │                             │
     │  Click "Register"              │                             │
     │───────────────────────────────►│                             │
     │  MessageType.Register          │                             │
     │                                │  AuthService.RegisterAsync()│
     │                                │──────────────────────────►  │
     │                                │  SELECT * FROM Users        │
     │                                │  WHERE Username = "john"    │
     │                                │◄── (returns existing user) ─│
     │                                │                             │
     │                                │  exists = true              │
     │                                │                             │
     │◄── BaseResponse {              │                             │
     │      Success=false,            │                             │
     │      Message="Username đã      │                             │
     │        tồn tại"}               │                             │
     │                                │                             │
     │  MessageBox("Username đã       │                             │
     │    tồn tại")                   │                             │
```

**Key Classes**: `MainWindow`, `AuthService`  
**Database Table**: `Users`

---

## Flow 10: Login Error - Wrong Password

```
User (Client)                       Server                       Database
     │                                │                             │
     │  Enter username: "john"        │                             │
     │  Enter password: "wrong"       │                             │
     │  Click "Login"                 │                             │
     │───────────────────────────────►│                             │
     │                                │  AuthService.LoginAsync()   │
     │                                │──────────────────────────►  │
     │                                │  SELECT * FROM Users        │
     │                                │  WHERE Username = "john"    │
     │                                │◄── (returns user record)   │
     │                                │                             │
     │                                │  BCrypt.Verify("wrong",    │
     │                                │    storedHash) → FALSE      │
     │                                │                             │
     │◄── BaseResponse {              │                             │
     │      Success=false,            │                             │
     │      Message="Sai mật khẩu"}   │                             │
     │                                │                             │
     │  MessageBox("Sai mật khẩu")    │                             │
```

**Key Classes**: `MainWindow`, `AuthService`  
**Database Table**: `Users`