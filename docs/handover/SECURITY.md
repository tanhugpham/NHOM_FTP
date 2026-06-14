# Security Documentation

## Overview

The application implements security at three layers:
1. **Authentication** - User identity verification (BCrypt)
2. **Data-in-transit encryption** - File chunk encryption (AES)
3. **Access control** - Per-user private storage + share code authorization

There is **no transport-layer security** (no TLS/SSL on the TCP connection).

---

## 1. Authentication

### Class: AuthService

**File**: `FileTransfer.Server/Services/AuthService.cs`

**Purpose**: Handles user registration and login with secure password hashing.

#### Registration Flow
1. Client sends `RegisterRequestDto` (Username, Password) as plaintext JSON
2. Server receives the credentials
3. Server checks if username already exists in `Users` table
4. If not, server hashes the password using `BCrypt.Net.BCrypt.HashPassword(password)`
5. Server stores `User` entity with `PasswordHash` field
6. Returns success/failure response

**Threat Mitigated**: **Password eavesdropping** (partial - password is sent in plaintext over TCP but hashed at rest). **Password compromise** if database is breached (BCrypt hash is computationally expensive to crack).

#### Login Flow
1. Client sends `LoginRequestDto` (Username, Password) as plaintext JSON
2. Server looks up user by username
3. Server verifies password using `BCrypt.Net.BCrypt.Verify(password, storedHash)`
4. If valid: creates `ClientSession` record, returns success
5. If invalid: returns failure message

**Threat Mitigated**: **Unauthorized access** - only users with correct credentials can log in. **Session tracking** - login creates a session record for audit.

---

## 2. Password Handling

### Technology: BCrypt.Net-Next 4.2.0

**BCrypt Features**:
- **Adaptive hash**: Salt is automatically generated and included in the hash output
- **Configurable work factor**: Uses default cost (10 rounds)
- **One-way hash**: Cannot be reversed; only verified via `BCrypt.Verify()`

### Where Passwords Are Used
| Location | Action |
|----------|--------|
| Client UI | User types password into `PasswordBox` (masked input) |
| Client memory | Password held in `string` variable temporarily |
| Network | Password sent as plaintext JSON over TCP |
| Server memory | Password received and hashed immediately |
| Database | Only BCrypt hash stored, never plaintext |

### Vulnerability: Plaintext Password on Network
Passwords are transmitted as **plaintext** inside JSON over TCP. There is no TLS. An attacker who can sniff the network can capture passwords. This is the most significant security weakness.

---

## 3. File Encryption (AES)

### Class: AesEncryptionHelper

**File**: `FileTransfer.Shared/Security/AesEncryptionHelper.cs`

**Purpose**: Encrypts file chunk data before transmission over the network and decrypts on the server side.

#### Encryption Details

| Parameter | Value |
|-----------|-------|
| Algorithm | AES (Rijndael) |
| Key Size | 256 bits (32 bytes) |
| Block Size | 128 bits (16 bytes) |
| Mode | CBC (default for System.Security.Cryptography.Aes) |
| Padding | PKCS7 (default) |
| Key | Hardcoded 32-byte array |
| IV | Hardcoded 16-byte array |

#### Static Key and IV (Hardcoded)

```csharp
private static readonly byte[] Key = {
    21, 12, 43, 54, 65, 76, 87, 98,
    10, 11, 22, 33, 44, 55, 66, 77,
    88, 99, 20, 30, 40, 50, 60, 70,
    80, 90, 15, 25, 35, 45, 55, 65
};

private static readonly byte[] IV = {
    11, 22, 33, 44, 55, 66, 77, 88,
    99, 10, 20, 30, 40, 50, 60, 70
};
```

#### Encrypt Method
1. Creates AES instance with static Key and IV
2. Creates encryptor
3. Writes plaintext data through CryptoStream
4. Returns encrypted byte array

#### Decrypt Method
1. Creates AES instance with same static Key and IV
2. Creates decryptor
3. Writes encrypted data through CryptoStream
4. Returns decrypted byte array

#### Where Encryption Is Applied

**Client (Upload)**:
```csharp
FileChunkDto chunkDto = new FileChunkDto
{
    FileId = fileId,
    ChunkData = AesEncryptionHelper.Encrypt(chunkData), // Encrypt before send
    ChunkIndex = chunkIndex,
    IsLastChunk = isLastChunk
};
```

**Server (Receive)**:
```csharp
byte[] decryptedChunk = AesEncryptionHelper.Decrypt(chunkDto.ChunkData); // Decrypt on receive
fs.Write(decryptedChunk, 0, decryptedChunk.Length);
```

**Threat Mitigated**: **Data-in-transit eavesdropping** - an attacker sniffing the TCP connection cannot read file contents because chunks are AES encrypted.

**Critical Vulnerability**: **Static key and IV are hardcoded in source code**. Anyone with access to the source code (or decompiled binaries) can decrypt the files. This is security-by-obscurity, not true security.

---

## 4. Access Control

### Private User Storage

**Purpose**: Ensure each user can only access their own files.

**Implementation**:
```csharp
private string GetUserStorageFolder(string username)
{
    string safeUsername = Path.GetFileName(username);
    string rootFolder = GetStorageFolder();
    string userFolder = Path.Combine(rootFolder, safeUsername);
    // Create if not exists
    Directory.CreateDirectory(userFolder);
    return userFolder;
}
```

**Key Point**: Username is sanitized via `Path.GetFileName()` to prevent path traversal attacks (e.g., `../../`).

**Threat Mitigated**: **Unauthorized file access** - users cannot access other users' storage directories. **Path traversal** - username is sanitized.

### Share Code Authorization

**Purpose**: Restrict shared file downloads to specific users.

**Implementation**:
```csharp
if (!string.IsNullOrWhiteSpace(sharedFile.AllowedUsername) &&
    sharedFile.AllowedUsername != currentUsername)
{
    return new BaseResponseDto {
        Success = false,
        Message = "Bạn không có quyền tải file này"
    };
}
```

**Threat Mitigated**: **Unauthorized sharing** - only the specified recipient can download a shared file.

---

## 5. Session Management

### Class: ClientSession (Entity)

**File**: `FileTransfer.Server/Entities/ClientSession.cs`

**Purpose**: Track user login sessions for basic audit.

| Field | Purpose |
|-------|---------|
| UserId | Which user logged in |
| ClientIp | Where the connection came from |
| ConnectedAt | When login occurred |
| IsOnline | Whether session is active |

**Current Issues**:
- Sessions are **never marked offline** when client disconnects
- `DisconnectedAt` is always null
- `IsOnline` is always true (never set to false)
- No session token or JWT - session is just an in-memory `Dictionary<TcpClient, string>`
- No session timeout or expiry

**Threat Mitigated**: Minimal - provides basic login audit trail.

---

## 6. Server-Side Security (Form1)

### Storage Path Handling

The server's `GetStorageFolder()` method determines the storage path relative to the application directory:
```csharp
string appFolder = AppDomain.CurrentDomain.BaseDirectory;
string storagePath = Path.Combine(appFolder, "Storage");
```

There is also a secondary method `GetStorageFolder()` in TcpServer that navigates up three parent directories to find the project root. This method is used for the upload file handlers.

**Threat Mitigated**: **Directory traversal** - all file operations use `Path.GetFileName()` to sanitize user-supplied filenames.

---

## 7. Summary of Security Protections

| Threat | Protection | Effectiveness |
|--------|------------|---------------|
| Password compromise at rest | BCrypt hashing | Strong (adaptive hash, salt included) |
| Password interception in transit | None (plaintext TCP) | **None** - critical weakness |
| File data interception in transit | AES-256 encryption | Moderate (key hardcoded) |
| Decompilation revealing encryption keys | None (hardcoded) | **None** - keys in source |
| Unauthorized user file access | Private storage folders | Strong |
| Path traversal attacks | Path.GetFileName() sanitization | Strong |
| Unauthorized shared file access | AllowedUsername validation | Strong |
| Session hijacking | No session tokens | **None** - session is socket-based |
| Brute force login | No rate limiting | **None** - no throttling |
| SQL injection | EF Core parameterized queries | Strong |
| Replay attacks | No nonce/timestamp | **None** - messages can be replayed |

---

## 8. Recommendations for Improvement

1. **Add TLS/SSL** to the TCP connection to encrypt all traffic (passwords included)
2. **Use per-session AES keys** exchanged via asymmetric encryption (like Diffie-Hellman or RSA)
3. **Move connection string** to configuration file (not hardcoded)
4. **Implement session tokens** (JWT or random tokens) instead of socket-based identity
5. **Add rate limiting** on login attempts to prevent brute force
6. **Implement share code deactivation** (delete or deactivate after use)
7. **Use `SecureString`** for passwords in memory (or at least clear them after use)
8. **Add activity audit** with timestamps and IP logging for all operations
9. **Close sessions** properly on disconnect (set IsOnline=false, DisconnectedAt=now)
10. **Add file integrity checks** (SHA-256 hash) to verify uploaded files