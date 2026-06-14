# Remaining Hardcoded Secrets Report

## Summary

| Severity | Count | Categories |
|---|---|---|
| 🔴 CRITICAL | 3 | DB credentials, AES keys committed to Git |
| 🟠 HIGH | 1 | Connection string exposed |
| 🟡 MEDIUM | 2 | Hardcoded file paths in source (pre-mTLS Phase 2C) |
| 🟢 LOW | 1 | Inconsistent README config |

**Total: 7 findings**

---

## Detailed Findings

### F1. MySQL Password Hardcoded in Connection String

| Field | Value |
|---|---|
| **File** | `FileTransfer.Server/Database/AppDbContext.cs` |
| **Line** | 17 |
| **Secret type** | Database password (`091103`) |
| **Severity** | 🔴 **CRITICAL** |
| **Risk** | Anyone with access to the source code (public GitHub repo) can connect to the production database. The password grants full root access to MySQL on localhost:3306. |
| **Code** | `"Server=localhost;Port=3306;Database=transferfile_mysql;User=root;Password=091103;SslMode=None;"` |
| **Recommend fix** | 1. Immediately rotate MySQL password. 2. Move connection string to `App.config` encrypted section. 3. Use `ConfigurationManager` to read at runtime. 4. Remove from Git history (`git filter-branch` or BFG Repo-Cleaner). |

### F2. MySQL Username Hardcoded in Connection String

| Field | Value |
|---|---|
| **File** | `FileTransfer.Server/Database/AppDbContext.cs` |
| **Line** | 17 |
| **Secret type** | Database username (`root`) |
| **Severity** | 🔴 **CRITICAL** |
| **Risk** | Combined with F1, grants full unrestricted access to MySQL. The `root` user has all privileges including `DROP DATABASE`, `GRANT`, and `CREATE USER`. |
| **Recommend fix** | 1. Create a dedicated MySQL user with minimal required privileges (CRUD on `transferfile_mysql` database only). 2. Never use `root` in application code. 3. Store in encrypted config. |

### F3. AES Encryption Key Hardcoded

| Field | Value |
|---|---|
| **File** | `FileTransfer.Shared/Security/AesEncryptionHelper.cs` |
| **Line** | 14-24 |
| **Secret type** | AES-256 secret key (32 bytes) |
| **Severity** | 🔴 **CRITICAL** |
| **Risk** | The symmetric encryption key is in the source code. Any attacker who decompiles the application or reads the GitHub repo can decrypt ALL file chunks uploaded via the system. AES key is shared between all users - no key isolation. |
| **Code** | `private static readonly byte[] Key = { 21, 12, 43, 54, 65, 76, 87, 98, 10, 11, 22, 33, 44, 55, 66, 77, 88, 99, 20, 30, 40, 50, 60, 70, 80, 90, 15, 25, 35, 45, 55, 65 };` |
| **Recommend fix** | 1. Remove hardcoded key from source. 2. Use per-user derived key (e.g., from user's password hash + salt via PBKDF2/RFC2898). 3. Or use Windows DPAPI to store key. 4. Never share same key across all users. |

### F4. AES IV Hardcoded

| Field | Value |
|---|---|
| **File** | `FileTransfer.Shared/Security/AesEncryptionHelper.cs` |
| **Line** | 26-32 |
| **Secret type** | AES initialization vector (16 bytes) |
| **Severity** | 🔴 **CRITICAL** |
| **Risk** | Static IV means identical plaintext chunks produce identical ciphertext. This leaks file structure information and enables pattern analysis attacks. Combined with hardcoded key (F3), the AES encryption provides near-zero security. |
| **Code** | `private static readonly byte[] IV = { 11, 22, 33, 44, 55, 66, 77, 88, 99, 10, 20, 30, 40, 50, 60, 70 };` |
| **Recommend fix** | 1. Generate random IV per encryption operation. 2. Prepend IV to ciphertext. 3. Extract IV during decryption. 4. Never reuse IV. |

### F5. Connection String Exposed in GitHub Repository

| Field | Value |
|---|---|
| **File** | Git history / `AppDbContext.cs` |
| **Line** | 17 (already committed) |
| **Secret type** | Full connection string with credentials |
| **Severity** | 🟠 **HIGH** |
| **Risk** | Even if the password is changed in the code, the old password remains in Git history. Anyone who clones the repo can run `git log -p` to find `Password=091103`. |
| **Recommend fix** | 1. Use `git filter-branch --tree-filter` or BFG Repo-Cleaner to remove the connection string from all commits. 2. Add `*.env` and `connectionStrings.*` to `.gitignore`. 3. Force push after cleanup. |

### F6. Hardcoded Certificate Paths (Partially Fixed)

| Field | Value |
|---|---|
| **File** | Removed from TcpServer.cs and TcpClientService.cs in Phase 2C ✅ |
| **Line** | N/A - already fixed |
| **Secret type** | File path configuration |
| **Severity** | 🟢 **LOW** (already remediated) |
| **Risk** | Previously hardcoded `"Certificates\\server.pfx"` etc. Now reads from `App.config`. ✅ |
| **Status** | ✅ **REMEDIATED** in Phase 2C |

### F7. Certificate Passwords in Environment Variables (Documented)

| Field | Value |
|---|---|
| **File** | `CertificateHelper.cs` (intentional design) |
| **Secret type** | PFX file passwords via env vars |
| **Severity** | 🟡 **MEDIUM** (by design) |
| **Risk** | Environment variables can be read by any process on the same machine. An attacker with local access can read `FT_SERVER_CERT_PASSWORD` and `FT_CLIENT_CERT_PASSWORD`. |
| **Recommend fix** | 1. For production, store PFX passwords in Windows Credential Manager or Azure Key Vault. 2. Document that env vars are development-only. 3. Consider using Windows Certificate Store instead of PFX files. |

### F8. AES Key/IV Not in `packages.config` or `.gitignore`

| Field | Value |
|---|---|
| **File** | `.gitignore` (missing entries) |
| **Secret type** | Missing configuration protection |
| **Severity** | 🟡 **MEDIUM** |
| **Risk** | If secrets are moved to config files in the future, they need `.gitignore` protection. Current `.gitignore` only has `*.env`, `appsettings.Development.json`, `Storage/`, `packages/`, `bin/`, `obj/`. |
| **Recommend fix** | Add to `.gitignore`: `*.pfx`, `*.pem`, `*.key`, `*.p12`, `secrets.*`, `connectionStrings.*`. |

---

## Recommended Remediation Order

| Priority | Finding | File | Effort | Dependencies |
|---|---|---|---|---|
| **P0** | F1 + F2: MySQL credentials hardcoded | `AppDbContext.cs:17` | Small (1h) | None |
| **P0** | F5: Remove from Git history | Git history | Small (1h) | F1 fix first (rotate password) |
| **P1** | F3 + F4: AES key/IV hardcoded | `AesEncryptionHelper.cs` | Medium (3h) | Requires per-user key derivation design |
| **P2** | F7: Env var security hardening | `CertificateHelper.cs` | Medium (2h) | After F3/F4 |
| **P3** | F8: Update `.gitignore` | `.gitignore` | Small (5min) | None |

## Summary: Severity Distribution

```
CRITICAL: ■■■■■■■■■■■■■■■■ 4 findings (F1, F2, F3, F4)
HIGH:     ■■■■■■■■■■■□□□□□ 1 finding  (F5)
MEDIUM:   ■■■■■■■□□□□□□□□□ 2 findings (F7, F8)
LOW:      ■■■□□□□□□□□□□□□□ 1 finding  (F6 - already fixed)
```

**Note:** F6 (hardcoded cert paths) was remediated in Phase 2C. All other findings are currently open.