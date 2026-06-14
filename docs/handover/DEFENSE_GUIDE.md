# Defense Guide: Likely Questions and Suggested Answers

## Architecture Questions

### Q1: Why did you use TCP Sockets instead of HTTP/REST?

**Answer**: We chose TCP sockets because the application requires real-time chunked file upload with resume capability. HTTP/REST would require additional overhead for each chunk (headers, handshakes). With raw TCP, we maintain a persistent connection and use a simple length-prefix framing protocol, which gives us:
- Lower latency per chunk (no HTTP header overhead)
- Full control over the message protocol
- Persistent connection for the entire session
- Simpler resume logic (same socket, just continue sending)

The trade-off is that we lose HTTP's built-in features like caching, content negotiation, and standardized error codes.

---

### Q2: Explain the 3-tier architecture of this project.

**Answer**: The application has three distinct tiers:
1. **Presentation Tier (Client)**: WPF application (FileTransfer.Client) handles all user interaction via MainWindow.xaml. It contains the login screen and main dashboard.
2. **Application Tier (Server)**: WinForms application (FileTransfer.Server) with TcpServer as the message router, Services for business logic, and EF Core for database access.
3. **Data Tier (Database)**: PostgreSQL hosted on Render.com, accessed only by the server through Entity Framework Core.

The shared library (FileTransfer.Shared) acts as a contract layer between client and server, defining DTOs, message types, encryption, and protocol helpers.

---

### Q3: Why is there a separate Shared library project?

**Answer**: The Shared library ensures both client and server use identical DTOs, message types, encryption logic, and protocol helpers. This prevents serialization mismatches and ensures type safety. Both projects reference the same library, so if we add a new message type or DTO, both sides automatically have it.

---

### Q4: How does the server handle multiple clients simultaneously?

**Answer**: The server uses `async/await` for non-blocking I/O. When `TcpListener.AcceptTcpClientAsync()` accepts a new client, the server fires `_ = HandleClientAsync(client)` as a fire-and-forget task. Each client gets its own async context with a dedicated NetworkStream. The server's main loop continues accepting new clients. This scales well because async I/O doesn't consume threads while waiting for network data.

---

### Q5: What happens if the server crashes during an upload?

**Answer**: The resume upload feature handles this. The server persists upload progress to the `FileTransferStates` table after each chunk. When the client reconnects, it sends a `ResumeCheckRequest` with the FileId (MD5 hash of filename + size + timestamp). The server returns the last successfully received chunk index and byte count. The client seeks to that position in the local file and resumes sending subsequent chunks. Note: This requires the client to still have the original file.

---

## Security Questions

### Q6: How are passwords stored?

**Answer**: Passwords are hashed using **BCrypt.Net-Next** (v4.2.0) before storage. The `AuthService.RegisterAsync()` method calls `BCrypt.Net.BCrypt.HashPassword(password)` which automatically generates a salt and includes it in the output hash. During login, `BCrypt.Verify(password, storedHash)` is used to verify. BCrypt is an adaptive hash function that can be made slower over time by increasing the work factor, making brute-force attacks impractical.

### Q7: Are there any security weaknesses in this application?

**Answer**: Yes, several:
1. **Passwords in plaintext over TCP**: Credentials are sent as unencrypted JSON. Network sniffers can capture them.
2. **Hardcoded AES keys**: The encryption key and IV are static and visible in source code. Anyone with the code can decrypt file chunks.
3. **No TLS/SSL**: The entire TCP connection is unencrypted. Only file chunk data is AES-encrypted, but metadata (filenames, commands) is visible.
4. **No rate limiting**: Attackers can brute-force login credentials without throttling.
5. **Session never closed**: `ClientSessions.DisconnectedAt` is never set; `IsOnline` is always true.
6. **Share codes never expire**: Once created, a share code is always active (IsActive is never set to false).

### Q8: How does AES encryption work in this project?

**Answer**: Each 64KB file chunk is encrypted client-side using `AesEncryptionHelper.Encrypt()` before being sent over TCP. The server decrypts it with `AesEncryptionHelper.Decrypt()` before appending to the file on disk. The algorithm is AES-256 with a hardcoded 32-byte key and 16-byte IV. Files on the server are stored in plaintext (decrypted). Encryption only protects data in transit, not at rest.

### Q9: How is access control enforced for user files?

**Answer**: Each user has a private storage directory named after their username (sanitized with `Path.GetFileName()` to prevent path traversal). File operations (upload, download, list) always use `GetUserStorageFolder(username)` which returns only that user's directory. The server uses a `Dictionary<TcpClient, string>` to map connected sockets to authenticated usernames, established during login.

### Q10: What threat does the `Path.GetFileName()` sanitization mitigate?

**Answer**: Path traversal attacks. If an attacker sends a filename like `../../etc/passwd`, `Path.GetFileName()` extracts just `passwd`, preventing access to files outside the intended directory. This is applied to:
- Usernames when creating storage folders
- Filenames during upload, download, and sharing

---

## Database Questions

### Q11: Why did you choose PostgreSQL over SQLite or SQL Server?

**Answer**: PostgreSQL was chosen for:
- **Production readiness**: Better concurrency, ACID compliance, and performance for multi-client scenarios
- **Cloud hosting**: PostgreSQL is widely supported by cloud providers (Render.com in this case)
- **JSON support**: While not used heavily, PostgreSQL's JSON support could be useful for future features
- **SSL support**: Built-in SSL/TLS for database connections
- **Cost**: The Render.com free tier PostgreSQL is sufficient for this scale

### Q12: Why is the connection string hardcoded in AppDbContext?

**Answer**: This is a development convenience, not a production best practice. In a production environment, the connection string should be in a configuration file (App.config / Web.config) with environment-specific values, or use environment variables / Azure Key Vault / etc. The hardcoded approach exposes credentials in source code, which is a security risk.

### Q13: Explain the purpose of the FileTransferStates table.

**Answer**: The `FileTransferStates` table enables the **resume upload** feature. It tracks:
- `FileId`: A deterministic MD5 hash identifying a specific file on a specific client machine
- `BytesReceived`: How many bytes have been successfully stored
- `LastChunkIndex`: The index of the last chunk written to disk
- `IsCompleted`: Whether the upload has finished

When a client reconnects after an interrupted upload, it checks this table to determine where to resume.

### Q14: Why don't TransferHistories and SharedFiles use foreign keys to Users?

**Answer**: This is a simplification choice. These tables store usernames as plain strings rather than foreign keys to `Users.Id`. This avoids complex joins and cascade operations. The trade-off is:
- **Pros**: Simpler queries, no foreign key constraints to manage
- **Cons**: No referential integrity - if a user is deleted, their history records become orphaned; username changes would break references

### Q15: Describe the migration strategy.

**Answer**: We use EF Core's code-first migrations. Migrations are created in chronological order:
1. InitialCreate (Users table)
2. AddClientSessions
3. AddTransferHistories
4. AddFileTransferStates
5. AddSharedFiles

To apply migrations: `Update-Database -Project FileTransfer.Server -StartupProject FileTransfer.Server`

---

## Networking Questions

### Q16: Explain the Length-Prefix TCP protocol.

**Answer**: Messages are framed with a 4-byte integer prefix indicating the length of the following JSON payload:
```
[4 bytes: message length (Int32)] [N bytes: UTF-8 JSON payload]
```

**Send**: Convert JSON to bytes, prepend length bytes via `BitConverter.GetBytes()`, write both to stream.
**Receive**: Read exactly 4 bytes, convert to Int32, read exactly that many bytes, convert to string, parse JSON.

This solves the TCP "message boundary" problem - TCP is a stream protocol with no concept of message boundaries. Without framing, the receiver wouldn't know where one message ends and the next begins.

### Q17: How does the server handle message routing?

**Answer**: The `TcpServer.HandleNetworkMessageAsync()` method acts as a message router. It receives a `NetworkMessage {Type, JsonBody}`, switches on the `Type` enum value, and dispatches to the appropriate handler method (HandleRegisterAsync, HandleLoginAsync, HandleFileStart, HandleFileChunk, etc.). Each handler deserializes the `JsonBody` to the appropriate DTO, performs business logic, and returns a response DTO. The response is serialized back to JSON and sent over the socket.

### Q18: What happens if the server stops while clients are connected?

**Answer**: The `TcpServer.Stop()` method cancels the CancellationTokenSource and stops the TcpListener. However, it does **not** explicitly close existing client connections. Those connections remain open until the client disconnects or the TCP timeout occurs. This is a design flaw - a proper implementation would iterate through connected clients and close their streams/sockets.

### Q19: How is file chunking implemented?

**Answer**: Files are split into 64KB (65536 byte) chunks. The client reads the file in a loop using `FileStream.Read(buffer, 0, 65536)`. Each chunk is:
1. Encrypted with `AesEncryptionHelper.Encrypt()`
2. Wrapped in a `FileChunkDto` (FileId, ChunkData, ChunkIndex, IsLastChunk)
3. Serialized to JSON and sent via TCP
4. Server decrypts and appends to the file on disk

The progress bar updates after each chunk: `percent = (totalSent * 100) / fileSize`.

---

## Design Decisions

### Q20: Why WPF for client and WinForms for server?

**Answer**: 
- **WPF Client**: Provides a modern, stylable UI with XAML. The client needed a polished interface with custom styles, data grids, progress bars, and responsive layout - WPF excels at this.
- **WinForms Server**: The server dashboard is simpler (status display, logs, control buttons). WinForms is lighter, faster to develop for simple UIs, and sufficient for administrative monitoring.

Both target .NET Framework 4.7.2 for compatibility.

### Q21: Why JSON instead of binary serialization?

**Answer**: JSON was chosen for:
- **Human-readable**: Easier to debug network traffic
- **Cross-platform**: JSON is universally supported if we later need non-.NET clients
- **Flexibility**: Newtonsoft.Json handles complex object graphs, polymorphism, and versioning
- **Familiarity**: Widely understood by developers
- **Tooling**: Excellent debugging tools (Wireshark, Fiddler can display JSON)

The trade-off is larger message sizes compared to binary formats like Protocol Buffers, but for this application, the overhead is acceptable.

### Q22: How does the application handle large file uploads?

**Answer**: Large files are handled through chunking. Instead of sending the entire file as one message (which would exceed memory limits and be very slow), the file is split into 64KB chunks. Each chunk is sent as a separate message. This approach:
- Prevents memory exhaustion on both client and server
- Allows progress tracking
- Enables resume (only re-send chunks that weren't saved)
- Keeps individual network messages small and fast

### Q23: What would you improve if you had more time?

**Answer**:
1. **Add TLS/SSL** to encrypt all TCP traffic
2. **Replace hardcoded AES keys** with per-session key exchange (Diffie-Hellman)
3. **Move connection string** to config file
4. **Implement proper session management** (JWT tokens, session expiry, proper disconnect handling)
5. **Add share code expiration** (one-time use or time-limited)
6. **Add file integrity verification** (SHA-256 hash after upload)
7. **Rate limiting** on login attempts
8. **Unit tests** for services and networking
9. **Logging framework** (Serilog/NLog) instead of UI listboxes
10. **Dependency injection** for better testability and loose coupling