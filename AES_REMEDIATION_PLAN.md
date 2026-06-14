# AES Security Remediation Plan

## 1. AES Implementation Review

### Source: `FileTransfer.Shared/Security/AesEncryptionHelper.cs`

| Property | Current Value | Security Status |
|---|---|---|
| **Algorithm** | `Aes.Create()` (default = **AES-CBC**) | ⚠️ CBC is secure but lacks authentication |
| **Key size** | 256-bit (32 bytes) | ✅ Adequate |
| **Key source** | Hardcoded `private static readonly byte[] Key` | 🔴 **CRITICAL** - identical for all users, in source code |
| **IV source** | Hardcoded `private static readonly byte[] IV` | 🔴 **CRITICAL** - static IV per chunk |
| **Padding** | PKCS7 (default) | ✅ Standard |
| **Authentication** | None | 🔴 No HMAC, no GCM - ciphertext can be tampered |
| **Mode** | CBC | ⚠️ OK but GCM preferred |

### Hardcoded Values

```csharp
// 32-byte key (all users, all files, forever)
private static readonly byte[] Key = {
    21, 12, 43, 54, 65, 76, 87, 98,
    10, 11, 22, 33, 44, 55, 66, 77,
    88, 99, 20, 30, 40, 50, 60, 70,
    80, 90, 15, 25, 35, 45, 55, 65
};

// 16-byte IV (static, reused for every chunk)
private static readonly byte[] IV = {
    11, 22, 33, 44, 55, 66, 77, 88,
    99, 10, 20, 30, 40, 50, 60, 70
};
```

---

## 2. Encryption Flow Analysis

### Upload Flow (Client Side)

```
MainWindow.btnUpload_Click()
  └─ UploadSingleFileAsync(filePath)
       ├─ Read chunk from disk (64KB)
       ├─ AesEncryptionHelper.Encrypt(chunkData)      ← HERE
       │    └─ Aes.Create()
       │    └─ aes.Key = Key (hardcoded)
       │    └─ aes.IV = IV (hardcoded)
       │    └─ CryptoStream(Encryptor) → ciphertext
       ├─ Create FileChunkDto { ChunkData = ciphertext }
       └─ SendRequestAsync(FileChunk, dto)
```

### Download Flow (Client Side)

```
MainWindow.btnDownload_Click()
  └─ SendDownloadRequestAsync(request)
       └─ Server sends DownloadFileResponseDto { FileData = byte[] }
       └─ File.WriteAllBytes(savePath, response.FileData)
       └─ ⚠ Files are NOT decrypted on download!
```

### Server Side (Chunk Reception)

```
TcpServer.HandleFileChunk()
  └─ AesEncryptionHelper.Decrypt(chunkDto.ChunkData)  ← HERE
       └─ Aes.Create()
       └─ aes.Key = Key (hardcoded)
       └─ aes.IV = IV (hardcoded)
       └─ CryptoStream(Decryptor) → plaintext
  └─ Append plaintext to disk file
```

### Call Chain Summary

| Direction | Caller | File | Line |
|---|---|---|---|
| **Encrypt** | `UploadSingleFileAsync()` | `MainWindow.xaml.cs` | 338 |
| **Decrypt** | `HandleFileChunk()` | `TcpServer.cs` | 368-369 |

---

## 3. Backward Compatibility Analysis

**Question:** If AES implementation changes, will previously uploaded files still be readable?

**Answer: ❌ NO**

**Why:**
- Uploaded files are stored as **decrypted plaintext** on disk (AES encrypted at transport, decrypted before write).
- The AES encryption is applied **per chunk during transit**, not stored encrypted on disk.
- Changing the AES key/IV will not affect previously uploaded files because they exist as plaintext on the server's storage.

```
Client                    Server                    Disk Storage
  │                         │                         │
  ├── AES.Encrypt(chunk)   │                         │
  │────TCP────────────────▶│                         │
  │                         ├── AES.Decrypt(encData) │
  │                         ├── Write plaintext ────▶│  ← Already plaintext
```

**However**, if we change the **encryption protocol** (e.g., from CBC to GCM, or add HMAC), the **Server's HandleFileChunk** method must be updated simultaneously with the Client's UploadSingleFileAsync. Both sides must agree on the protocol.

**Migration strategy:** Version the encryption scheme:
- V1 = current (CBC, hardcoded key/IV) - legacy support only
- V2 = new (per-user derived key, random IV per chunk, GCM with authentication)
- Files stored on disk remain plaintext regardless of version

---

## 4. Remediation Options

### Option A: Minimal Change (Secure the Key)

| Property | Value |
|---|---|
| **Security level** | 🟡 Medium |
| **Complexity** | Low |
| **Migration effort** | 2 hours |
| **Backward compatibility** | ✅ Full |

**Changes:**
1. Move AES key/IV to environment variables (`FT_AES_KEY`, `FT_AES_IV`)
2. Read them via `Environment.GetEnvironmentVariable()` at startup
3. Generate random key on first run and document export
4. Keep CBC mode + PKCS7 padding

**Pros:**
- Minimal code changes
- Key no longer in source code
- All existing files remain decryptable (same algorithm, key just moved)
- No changes to upload/download business logic

**Cons:**
- Key still shared across all users
- IV still static
- No authentication (no tamper detection)
- CBC mode (no AAD/GCM)

---

### Option B: Secure Modern Design (Per-User Key + Random IV + HMAC)

| Property | Value |
|---|---|
| **Security level** | 🟢 High |
| **Complexity** | Medium |
| **Migration effort** | 4-6 hours |
| **Backward compatibility** | ⚠️ Partial (need version header) |

**Changes:**

1. **Key derivation:** Derive per-user key from `BCrypt.Verify(password) + user-specific salt` using `Rfc2898DeriveBytes` (PBKDF2)
2. **Random IV:** Generate `Aes.GenerateIV()` per encryption call. Prepend IV to ciphertext.
3. **Authentication:** Append HMAC-SHA256 for tamper detection
4. **Protocol version:** First byte = version number (0x01 = current, 0x02 = new)
5. **Still AES-CBC** (built-in, no extra dependencies)

**Chunk format (V2):**
```
[1 byte version][16 bytes IV][remaining bytes ciphertext][32 bytes HMAC]
```

**Upload flow (V2):**
```
Client:
  key = PBKDF2(user_password, salt, iterations)
  iv = Random()
  ciphertext = AES-CBC-Encrypt(key, iv, plaintext)
  hmac = HMAC-SHA256(key, iv || ciphertext)
  transmit = version || iv || ciphertext || hmac

Server:
  key = PBKDF2(user_password, salt, iterations)  ← same derivation
  iv = transmit[1..17]
  Verify HMAC
  plaintext = AES-CBC-Decrypt(key, iv, ciphertext[17..-32])
  Write plaintext to disk
```

**Pros:**
- Per-user key isolation (compromise of one user ≠ compromise of all)
- Random IV per chunk (no ECB-like patterns)
- HMAC authentication (tamper detection)
- Key never stored anywhere (derived from password)

**Cons:**
- Server needs user's password to derive key → password must be stored or re-derived
- Cannot derive from BCrypt hash (BCrypt is one-way)
- Changes chunk format → need version negotiation
- More complex error handling

---

### Option C: Enterprise Grade (AES-GCM + Key Management + Rotation)

| Property | Value |
|---|---|
| **Security level** | 🟢🟢 Maximum |
| **Complexity** | High |
| **Migration effort** | 2-3 days |
| **Backward compatibility** | ⚠️ Partial |

**Changes:**

1. **AES-GCM mode** (authenticated encryption built into .NET Framework 4.7.2's `System.Security.Cryptography.AesGcm` - requires `System.Security.Cryptography` namespace)
2. **Key from Windows Certificate Store** or Azure Key Vault - never in code or env vars
3. **Per-file random nonce** (12 bytes) prepended to ciphertext
4. **Associated Data** (AAD) = file ID + chunk index for integrity
5. **Key rotation** support via key ID header

**Chunk format (V3):**
```
[1 byte version][1 byte keyId][12 bytes nonce][remaining bytes ciphertext + tag]
```

**Pros:**
- GCM provides authenticated encryption (confidentiality + integrity)
- No separate HMAC needed
- Key management via Windows Certificate Store (hardware-backed option)
- Key rotation without re-encrypting all data
- Industry standard (FIPS 140-2 compliant)

**Cons:**
- `AesGcm` requires .NET Framework 4.7.2+ (✅ current target)
- Significantly more code changes
- Certificate Store adds deployment complexity
- Key rotation requires tracking which key was used for which chunk
- Overkill for this project's threat model

---

## 5. Recommendation

### ✅ Recommend: **Option A - Minimal Change**

**Why Option A fits this project:**

| Factor | Assessment |
|---|---|
| **Project maturity** | MVP - core features still being stabilized |
| **Threat model** | Internal/private use, not multi-tenant SaaS |
| **Existing hardcoded key** | Single key for all users - moving to env var is the minimum acceptable fix |
| **Files are plaintext on disk** | AES only protects transport - mTLS (already implemented) provides transport security |
| **mTLS already deployed** | All traffic is encrypted via SslStream - AES is defense-in-depth, not primary |
| **Effort** | 2 hours vs 4-6 hours for Option B |
| **Build impact** | No new NuGet packages, no version negotiation, no chunk format changes |

**Implementation plan (2 hours):**

| Step | Change | Effort |
|---|---|---|
| 1 | Generate random AES-256 key + 16-byte IV via `RandomNumberGenerator.Create()` | 15 min |
| 2 | Move to `FT_AES_KEY` (base64) and `FT_AES_IV` (base64) environment variables | 15 min |
| 3 | Modify `AesEncryptionHelper.cs` to read from env vars on first use | 30 min |
| 4 | Add `Encrypt()` and `Decrypt()` overloads that accept explicit key/IV (future-proofing) | 30 min |
| 5 | Update documentation (`AES_REMEDIATION_PLAN.md` → `docs/AES_SECRET_SETUP.md`) | 15 min |
| 6 | Build + test + verify backward compatibility | 15 min |

**Risks:**
- None - existing encryption flow unchanged, only key source changes
- All previously uploaded files remain readable (plaintext on disk)
- No protocol changes needed

**Why NOT Option B or C for now:**
- File chunks are **transported over mTLS** (implemented in Phase 2B) - AES is already layered on top of TLS, which provides encryption + authentication. The hardcoded key is the real vulnerability, not the algorithm.
- Per-user key derivation requires storing password in a way the server can reproduce (plaintext or reversible) - this weakens the password security BCrypt was designed to provide.
- Enterprise GCM + Key Vault is disproportionate for the current project stage.