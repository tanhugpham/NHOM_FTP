# Database Secret Setup Guide

## Architecture

The database connection string is built dynamically at runtime from:

```
App.config  (server, database name, username)
    +
Environment Variable  (password only)
```

No credentials are hardcoded in source code.

## Configuration

### Step 1: App.config

Edit `FileTransfer.Server/App.config`:

```xml
<appSettings>
    <!-- Existing mTLS settings -->
    <add key="ServerCertificatePath" value="Certificates\server.pfx" />
    <add key="CACertificatePath" value="Certificates\ca.cer" />

    <!-- Database settings (no password here) -->
    <add key="DbServer" value="localhost" />
    <add key="DbName" value="transferfile_mysql" />
    <add key="DbUser" value="root" />
</appSettings>
```

| Key | Description | Default |
|---|---|---|
| `DbServer` | MySQL host | `localhost` |
| `DbName` | Database name | `transferfile_mysql` |
| `DbUser` | Database username | `root` (create dedicated user for production) |

### Step 2: Environment Variable

Set the `FT_DB_PASSWORD` environment variable.

**Windows (Command Prompt):**
```cmd
set FT_DB_PASSWORD=YourActualPassword
```

**Windows (PowerShell):**
```powershell
$env:FT_DB_PASSWORD = "YourActualPassword"
```

**Persistent (System environment):**
```cmd
setx FT_DB_PASSWORD "YourActualPassword"
```

### Step 3: Verify

```cmd
echo %FT_DB_PASSWORD%
```

## Startup Behavior

When the server starts:

1. `AppDbContext.OnConfiguring()` is called by EF Core
2. `BuildConnectionString()` reads `DbServer`, `DbName`, `DbUser` from `App.config`
3. `BuildConnectionString()` reads `FT_DB_PASSWORD` from environment
4. If any value is missing → `InvalidOperationException` with clear message
5. Connection string is built dynamically and passed to `UseMySql()`

## Error Messages

| Missing Value | Error Message |
|---|---|
| `DbServer` not in App.config | "DbServer is not configured in App.config." |
| `DbName` not in App.config | "DbName is not configured in App.config." |
| `DbUser` not in App.config | "DbUser is not configured in App.config." |
| `FT_DB_PASSWORD` not set | "FT_DB_PASSWORD environment variable is not set." |

No error message contains the actual password value.

## Migration Notes

### If migrating from hardcoded connection string:

1. **Rotate the database password first** (old password `091103` is in Git history)
2. Create a dedicated MySQL user with minimal privileges:

```sql
CREATE USER 'filetransfer_app'@'localhost' IDENTIFIED BY 'NewSecurePassword';
GRANT SELECT, INSERT, UPDATE, DELETE ON transferfile_mysql.* TO 'filetransfer_app'@'localhost';
FLUSH PRIVILEGES;
```

3. Update `App.config`:

```xml
<add key="DbUser" value="filetransfer_app" />
```

4. Set environment variable:

```cmd
setx FT_DB_PASSWORD "NewSecurePassword"
```

5. Remove `root` user from App.config

### Git History Cleanup

The old connection string (`Password=091103`) exists in Git history. To remove it:

```bash
# Install BFG Repo-Cleaner (Java required)
java -jar bfg.jar --replace-text passwords.txt FileTransfer.Server/Database/AppDbContext.cs

# Or use git filter-branch
git filter-branch --tree-filter "sed -i 's/Password=091103/Password=REMOVED/g' FileTransfer.Server/Database/AppDbContext.cs" HEAD

# Force push
git push --force --all
```

## Verification

```cmd
# Set password
set FT_DB_PASSWORD=YourActualPassword

# Run server
FileTransfer.Server.exe

# Expected: server starts, connects to MySQL successfully
# Expected: no "FT_DB_PASSWORD environment variable is not set" error
```

## Security Notes

- The DB password **never** appears in source code
- The DB password **never** appears in App.config
- The DB password is **never** logged or included in exception messages
- For production, use a dedicated MySQL user with minimal privileges (not `root`)
- Consider Windows Credential Manager or Azure Key Vault for production password storage