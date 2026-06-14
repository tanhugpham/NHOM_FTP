# AES Secret Setup Guide

## Architecture

The AES-256 encryption key and initialization vector (IV) are loaded from environment variables at startup.

```
FT_AES_KEY  ──▶ AesEncryptionHelper (static constructor)
FT_AES_IV   ──▶     │
                     ├─ Validate: key == 32 bytes (256-bit)
                     ├─ Validate: IV  == 16 bytes (128-bit)
                     └─ Fail fast if missing or invalid
```

## Requirements

| Variable | Length | Format | Description |
|---|---|---|---|
| `FT_AES_KEY` | 32 bytes (256-bit) | Base64 (44 chars) | AES encryption key |
| `FT_AES_IV` | 16 bytes (128-bit) | Base64 (24 chars) | AES initialization vector |

## Generating Keys

### PowerShell
```powershell
# Generate random 32-byte key and encode as Base64
$key = [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))
$iv  = [Convert]::ToBase64String((1..16 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

Write-Host "FT_AES_KEY=$key"
Write-Host "FT_AES_IV=$iv"
```

### .NET (C#)
```csharp
using System.Security.Cryptography;

byte[] key = new byte[32];
byte[] iv = new byte[16];
RandomNumberGenerator.Create().GetBytes(key);
RandomNumberGenerator.Create().GetBytes(iv);

string keyBase64 = Convert.ToBase64String(key);
string ivBase64 = Convert.ToBase64String(iv);

Console.WriteLine($"FT_AES_KEY={keyBase64}");
Console.WriteLine($"FT_AES_IV={ivBase64}");
```

### Python
```python
import os, base64

key = base64.b64encode(os.urandom(32)).decode()
iv  = base64.b64encode(os.urandom(16)).decode()

print(f"FT_AES_KEY={key}")
print(f"FT_AES_IV={iv}")
```

## Setting Environment Variables

### Windows (Command Prompt)
```cmd
set FT_AES_KEY=YOUR_BASE64_ENCODED_32_BYTE_KEY
set FT_AES_IV=YOUR_BASE64_ENCODED_16_BYTE_IV
```

### Windows (PowerShell)
```powershell
$env:FT_AES_KEY = "YOUR_BASE64_ENCODED_32_BYTE_KEY"
$env:FT_AES_IV  = "YOUR_BASE64_ENCODED_16_BYTE_IV"
```

### Persistent (System Environment)
```cmd
setx FT_AES_KEY "YOUR_BASE64_ENCODED_32_BYTE_KEY"
setx FT_AES_IV  "YOUR_BASE64_ENCODED_16_BYTE_IV"
```

## Verification

After setting the variables, verify they are accessible:

```cmd
echo %FT_AES_KEY%
echo %FT_AES_IV%
```

Each should print a Base64 string:
- `FT_AES_KEY`: 44 characters ending with `==`
- `FT_AES_IV`: 24 characters ending with `==`

## Error Messages

| Problem | Error |
|---|---|
| `FT_AES_KEY` not set | "FT_AES_KEY environment variable is not set." |
| `FT_AES_IV` not set | "FT_AES_IV environment variable is not set." |
| Key not valid Base64 | "FT_AES_KEY is not valid Base64." |
| IV not valid Base64 | "FT_AES_IV is not valid Base64." |
| Key not 32 bytes | "FT_AES_KEY must be exactly 32 bytes." |
| IV not 16 bytes | "FT_AES_IV must be exactly 16 bytes." |

No error message exposes the actual key or IV values.

## Migration from Hardcoded Values

The old hardcoded values are no longer used. If you need to maintain backward compatibility with previously encrypted data, generate new keys.

**Note:** Files are stored decrypted on disk. AES only protects transport between client and server. Changing the key does not affect previously uploaded files.

## Security Notes

- **Never** commit `FT_AES_KEY` or `FT_AES_IV` to source control
- **Never** log the key or IV values
- **Never** hardcode fallback values in source code
- Generate unique keys per deployment (do not reuse across environments)
- Rotate keys periodically (every 90 days recommended for production)
- Environment variables can be read by any process on the same machine - for production, consider Windows Credential Manager or Azure Key Vault