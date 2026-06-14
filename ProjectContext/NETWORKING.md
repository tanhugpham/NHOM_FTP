# Networking Documentation

## Communication Architecture

The application uses **raw TCP Sockets** for all client-server communication. There is no HTTP, no REST API, and no higher-level protocol framework. The networking layer is custom-built from `System.Net.Sockets.TcpClient` and `TcpListener`.

## Protocol

### Length-Prefixed JSON Protocol

Messages are sent over TCP using a **length-prefix framing protocol**:

```
[4-byte length (Int32)] [UTF-8 JSON payload]
```

**Encoding**:
1. Serialize the `NetworkMessage` object to JSON using Newtonsoft.Json
2. Convert JSON string to UTF-8 bytes
3. Prepend 4 bytes containing the length of the JSON bytes (Big-endian/little-endian depends on architecture, but uses `BitConverter.GetBytes`)
4. Send the complete byte array over the TCP stream

**Decoding**:
1. Read exactly 4 bytes from the stream
2. Convert to Int32 using `BitConverter.ToInt32()` to get message length
3. Read exactly `messageLength` bytes from the stream
4. Convert UTF-8 bytes to string
5. Deserialize JSON to `NetworkMessage` object

### Message Format

Every message follows this structure:

```json
{
  "Type": "Login|Register|FileStart|FileChunk|...",
  "JsonBody": "{...serialized DTO...}"
}
```

### Message Types (Enum: MessageType)

| Value | Name | Direction | Purpose |
|-------|------|-----------|---------|
| 0 | Ping | Both | Heartbeat (defined but unused) |
| 1 | Register | Client→Server | Create account |
| 2 | Login | Client→Server | Authenticate |
| 3 | FileUpload | Both | (Defined but unused - replaced by FileStart/Chunk/Complete) |
| 4 | FileStart | Client→Server | Begin file upload |
| 5 | FileChunk | Client→Server | Send encrypted chunk |
| 6 | FileComplete | Client→Server | Mark upload complete |
| 7 | ResumeCheck | Client→Server | Check upload state for resume |
| 8 | Error | Server→Client | Error notification |
| 9 | GetFileList | Client→Server | Request user's file list |
| 10 | DownloadFile | Client→Server | Request file download |
| 11 | CreateShareCode | Client→Server | Generate share code |
| 12 | DownloadSharedFile | Client→Server | Download shared file |

## Request Flow

### Client Side (TcpClientService)

```csharp
public class TcpClientService
{
    TcpClient _client;
    NetworkStream _stream;

    // 1. Connect
    await _client.ConnectAsync(ip, port);
    _stream = _client.GetStream();

    // 2. Send/Receive
    await TcpMessageHelper.SendStringAsync(_stream, json);
    string response = await TcpMessageHelper.ReadStringAsync(_stream);
    return response;
    
    // 3. Disconnect
    _stream.Close();
    _client.Close();
}
```

### Server Side (TcpServer)

```csharp
public class TcpServer
{
    TcpListener _listener;
    
    // 1. Start listening
    _listener = new TcpListener(IPAddress.Any, port);
    _listener.Start();
    
    // 2. Accept clients in loop
    while (!_cts.IsCancellationRequested)
    {
        TcpClient client = await _listener.AcceptTcpClientAsync();
        _ = HandleClientAsync(client); // Fire-and-forget
    }
    
    // 3. Handle each client
    async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        while (client.Connected)
        {
            string json = await TcpMessageHelper.ReadStringAsync(stream);
            var msg = JsonHelper.Deserialize<NetworkMessage>(json);
            var response = await HandleNetworkMessageAsync(msg, client, ip);
            await TcpMessageHelper.SendStringAsync(stream, JsonHelper.Serialize(response));
        }
    }
}
```

## Response Flow

Every request receives exactly **one response**. The protocol is synchronous request-response (not streaming).

### Base Response Format

```json
{
  "Success": true,
  "Message": "Operation result message"
}
```

### Specialized Response Formats

**File List Response**:
```json
{
  "Success": true,
  "Message": "Lấy danh sách file thành công",
  "Files": [
    { "FileName": "report.pdf", "FileSize": 1024000 },
    { "FileName": "photo.jpg", "FileSize": 2048000 }
  ]
}
```

**Download Response**:
```json
{
  "Success": true,
  "Message": "Download file thành công",
  "FileName": "report.pdf",
  "FileData": "base64-encoded-bytes"
}
```

**Resume Check Response**:
```json
{
  "Success": true,
  "Message": "Resume state found",
  "LastChunkIndex": 5,
  "BytesReceived": 393216,
  "IsCompleted": false
}
```

**Create Share Code Response**:
```json
{
  "Success": true,
  "Message": "Tạo mã chia sẻ thành công",
  "ShareCode": "A1B2C3D4"
}
```

## Socket Architecture

```
┌─────────────────────────────────────────────┐
│              Master Socket                   │
│  TcpListener (IPAddress.Any, Port 9000)      │
│      AcceptTcpClientAsync()                  │
└─────────────────┬───────────────────────────┘
                  │
    ┌─────────────┼──────────────┐
    ▼             ▼              ▼
┌─────────┐ ┌─────────┐   ┌─────────┐
│ Client1 │ │ Client2 │   │ ClientN │
│ Socket  │ │ Socket  │   │ Socket  │
└────┬────┘ └────┬────┘   └────┬────┘
     │           │             │
     ▼           ▼             ▼
┌─────────┐ ┌─────────┐   ┌─────────┐
│ Network │ │ Network │   │ Network │
│ Stream  │ │ Stream  │   │ Stream  │
└─────────┘ └─────────┘   └─────────┘
```

- **Server**: Single `TcpListener` accepts multiple clients concurrently
- **Each client**: Gets a dedicated `TcpClient` + `NetworkStream` 
- **Async handling**: Each client is handled in a fire-and-forget task via `_ = HandleClientAsync(client)`
- **No thread pool**: Each client runs on its own async context (not dedicated threads)
- **Client tracking**: Server maintains a `Dictionary<TcpClient, string>` mapping sockets to usernames

## Data Flow Examples

### Upload Flow (Chunked)
```
CLIENT                              SERVER
  │                                   │
  │── ResumeCheck(FileId) ───────────►│── Lookup FileTransferState
  │◄── ResumeCheckResponse ──────────│
  │                                   │
  │── FileStart(FileId,Name,Size) ──►│── Create empty file
  │◄── BaseResponse(OK) ─────────────│── Save FileTransferState
  │                                   │
  │── FileChunk(Id,EncryptedData,Idx)►│── Decrypt + Append to file
  │◄── BaseResponse(OK) ─────────────│── Update FileTransferState
  │  (repeat for all chunks)          │
  │                                   │
  │── FileComplete(FileId,Name) ────►│── Mark complete
  │◄── BaseResponse(OK) ─────────────│── Log to TransferHistory
```

### Download Flow
```
CLIENT                              SERVER
  │                                   │
  │── DownloadFile(FileName) ───────►│── Read file from storage
  │◄── DownloadFileResponse(Data) ───│── Log to TransferHistory
  │                                   │
  │  (Write bytes to local file)      │
```

### Share Code Flow
```
OWNER CLIENT                        SERVER
  │                                   │
  │── CreateShareCode(File,User) ───►│── Validate file exists
  │◄── CreateShareCodeResponse(Code) │── Save SharedFile to DB
  │                                   │
  │  (Share code via external chat)   │
  │                                   │
RECIPIENT CLIENT                    
  │── DownloadSharedFile(Code) ─────►│── Lookup SharedFile
  │◄── DownloadFileResponse(Data) ───│── Validate AllowedUsername
  │                                   │── Read from owner's storage
  │                                   │── Log to TransferHistory
```

## Important Implementation Details

### Message Size Limits
- The 4-byte length prefix allows messages up to ~2GB (Int32 max)
- However, in practice, FileChunk bodies are limited to 64KB + encryption overhead
- DownloadFile responses can be large (entire file bytes in base64)

### Connection Lifecycle
1. Client connects → Server adds to `_clientUsers` dict on login
2. Client performs operations
3. Client disconnects → Server removes from dict and closes socket
4. No keep-alive or heartbeat (Ping type defined but not used)

### Security Considerations
- Messages are JSON serialized (readable if intercepted)
- File data is AES-encrypted **inside** the JSON body (double-encoded)
- No TLS/SSL on the TCP connection itself
- No authentication on connection - only on login message

## Threading Model

- Server uses async/await throughout (not threads)
- `HandleClientAsync` runs as fire-and-forget: `_ = HandleClientAsync(client)`
- UI updates use `Invoke` for thread safety (WinForms)
- No synchronization mechanisms for shared state (`_clientUsers`, `_uploadingFiles`) beyond single-threaded async context