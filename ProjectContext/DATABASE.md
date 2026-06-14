# Database Documentation

## Overview

The database is a **PostgreSQL** instance hosted on **Render.com** (cloud). The application uses **Entity Framework Core 3.1.32** with the **Npgsql** provider for all database operations. Migrations are managed via EF Core's code-first migration system.

## Connection Configuration

- **Host**: `dpg-d88leh6gvqtc73b7osb0-a.singapore-postgres.render.com`
- **Port**: 5432
- **Database**: `transferfile`
- **SSL Mode**: Require
- **Trust Server Certificate**: true

Connection string is hardcoded in `AppDbContext.OnConfiguring()`.

## Tables

### 1. Users

**Purpose**: Stores registered user accounts.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int (serial) | Primary Key, Auto-increment | Unique user identifier |
| Username | text | Not null | Login username |
| PasswordHash | text | Not null | BCrypt hash of the password |
| CreatedAt | timestamp | Not null | Account creation timestamp |

**Relationships**: One-to-many with `ClientSessions` (via UserId)

**Important Notes**:
- Username is used for lookups (no unique constraint visible in entity, but application checks existence before insert)
- Passwords are never stored in plaintext - always BCrypt hashed

---

### 2. ClientSessions

**Purpose**: Tracks user login sessions (who connected, when, from where).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int (serial) | Primary Key, Auto-increment | Session identifier |
| UserId | int | Foreign Key → Users.Id | Which user logged in |
| ClientIp | text | Not null | IP address of the connecting client |
| ConnectedAt | timestamp | Not null | When the session started |
| DisconnectedAt | timestamp | Nullable | When the session ended (not currently set) |
| IsOnline | bool | Not null | Whether session is still active |

**Relationships**: Many-to-one with `Users`

**Important Notes**:
- A new session is created on every login
- DisconnectedAt is never updated (remains null) - sessions are never properly closed
- IsOnline is always set to `true` on creation and never set to `false`

---

### 3. FileTransferStates

**Purpose**: Tracks upload progress for resume support.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int (serial) | Primary Key, Auto-increment | State record identifier |
| FileId | text | Not null | MD5 hash-based unique file identifier |
| FileName | text | Not null | Original filename |
| TotalBytes | bigint | Not null | Total file size in bytes |
| BytesReceived | bigint | Not null | Bytes received so far |
| LastChunkIndex | int | Not null | Index of last received chunk (-1 if none) |
| IsCompleted | bool | Not null | Whether upload is finished |
| UpdatedAt | timestamp | Not null | Last progress update timestamp |

**Relationships**: None (standalone table)

**Important Notes**:
- FileId is generated client-side from MD5(fileName + fileSize + lastWriteTime)
- Used for resume upload: if IsCompleted=false, server tells client to resume from LastChunkIndex + 1
- When FileComplete is called, IsCompleted is set to true

---

### 4. TransferHistories

**Purpose**: Audit log of all file transfers (uploads, downloads, shared downloads).

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int (serial) | Primary Key, Auto-increment | Log entry identifier |
| Username | text | Not null | User who performed the transfer |
| FileName | text | Not null | Name of the transferred file |
| FileSize | bigint | Not null | Size of the file in bytes |
| TransferType | text | Not null | Type: "Upload", "Download", or "DownloadShared" |
| Status | text | Not null | Status: "Success" |
| CreatedAt | timestamp | Not null | When the transfer occurred |

**Relationships**: None (standalone table)

**Important Notes**:
- All transfer types use UTC timestamps
- Admin can clear this table from the dashboard via `AdminCleanupService.ClearLogsAsync()`
- Status is always "Success" (failure logs are not recorded)

---

### 5. SharedFiles

**Purpose**: Stores share codes generated for file sharing between users.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Id | int (serial) | Primary Key, Auto-increment | Share record identifier |
| OwnerUsername | text | Not null | Username of the file owner |
| FileName | text | Not null | Name of the shared file |
| ShareCode | text | Not null | 8-character uppercase code (GUID-based) |
| AllowedUsername | text | Not null | Only this user can download (or empty) |
| CreatedAt | timestamp | Not null | When the share code was created |
| IsActive | bool | Not null | Whether the share is still valid |

**Relationships**: None (standalone table)

**Important Notes**:
- ShareCode is an 8-character uppercase string derived from `Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()`
- `AllowedUsername` is required (the owner must specify who can download)
- `IsActive` is always set to `true` and never set to `false` (no deactivation mechanism)
- The same file can be shared multiple times, generating different share codes
- There is no unique constraint on ShareCode (collision is statistically unlikely)

## Entity Relationship Summary

```
Users (1) ──────── (N) ClientSessions
  │
  │  (No direct FK relationships)
  │
  ├── FileTransferStates (standalone, referenced by FileId string)
  ├── TransferHistories (standalone, stores username as string)
  └── SharedFiles (standalone, stores owner username as string)
```

**Note**: The entities `TransferHistories` and `SharedFiles` reference users by **string username** rather than by **foreign key to Users.Id**. This is a design choice that sacrifices referential integrity for simplicity.

## Migrations

**Total Migrations**: 5 (in chronological order)

### Migration 1: `20260523073319_InitialPostgresCreate`
- Creates `Users` table with all columns

### Migration 2: `20260523075039_AddClientSessions`
- Creates `ClientSessions` table
- Adds foreign key relationship to Users table

### Migration 3: `20260523090715_AddTransferHistories`
- Creates `TransferHistories` table

### Migration 4: `20260523092228_AddFileTransferStates`
- Creates `FileTransferStates` table

### Migration 5: `20260523112931_AddSharedFiles`
- Creates `SharedFiles` table

All migrations were created on the same date (May 23, 2026), suggesting the database schema was developed in a single session.

## Database Context (AppDbContext)

The `AppDbContext` class:
- Extends `Microsoft.EntityFrameworkCore.DbContext`
- Exposes 5 DbSet properties matching the 5 tables
- Configures PostgreSQL via `UseNpgsql()` with hardcoded connection string
- No explicit model configuration using Fluent API (relies on conventions)

## Entity Framework Core Details

- **Version**: 3.1.32 (compatible with .NET Framework 4.7.2)
- **Provider**: Npgsql.EntityFrameworkCore.PostgreSQL 3.1.18
- **Design Package**: Microsoft.EntityFrameworkCore.Design 3.1.32 (for migrations)

## Issues / Observations

1. **Connection string is hardcoded** in source code with credentials exposed - a security concern for public repositories.
2. **Sessions are never closed**: `DisconnectedAt` is never set, `IsOnline` is never set to false.
3. **No cascade delete**: Deleting a user would orphan associated sessions.
4. **Username-based references** instead of FK: `TransferHistories` and `SharedFiles` use string usernames rather than foreign keys, which could cause issues if usernames change.
5. **No share code revocation**: Once created, a share code is always active.
6. **Timestamps**: Some entities use `DateTime.Now` (local time), others use `DateTime.UtcNow` - inconsistent.