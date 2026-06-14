-- =============================================================
-- MySQL Schema for Secure File Transfer Client-Server
-- Target: MySQL 8.0+
-- Engine: InnoDB (supports transactions + FK)
-- Charset: utf8mb4 (full Unicode support)
-- =============================================================

CREATE DATABASE IF NOT EXISTS transferfile_mysql
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE transferfile_mysql;

-- =============================================================
-- TABLE: Users
-- Stores registered user accounts.
-- PasswordHash is a BCrypt hash (always 60 characters).
-- =============================================================
CREATE TABLE IF NOT EXISTS Users (
    Id          INT             NOT NULL AUTO_INCREMENT,
    Username    VARCHAR(255)    NOT NULL,
    PasswordHash VARCHAR(255)   NOT NULL,
    CreatedAt   DATETIME        NOT NULL,
    PRIMARY KEY (Id),
    CONSTRAINT UQ_Users_Username UNIQUE (Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- TABLE: ClientSessions
-- Tracks user login sessions.
-- DisconnectedAt is NULL until session is closed (future use).
-- IsOnline: TINYINT(1) acts as boolean (1 = online, 0 = offline).
-- =============================================================
CREATE TABLE IF NOT EXISTS ClientSessions (
    Id              INT             NOT NULL AUTO_INCREMENT,
    UserId          INT             NOT NULL,
    ClientIp        VARCHAR(45)     NOT NULL,
    ConnectedAt     DATETIME        NOT NULL,
    DisconnectedAt  DATETIME        NULL,
    IsOnline        TINYINT(1)      NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT FK_ClientSessions_Users
        FOREIGN KEY (UserId) REFERENCES Users (Id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- TABLE: FileTransferStates
-- Tracks upload progress to support resume upload.
-- FileId: MD5 hash from client (filename + size + timestamp).
-- LastChunkIndex: -1 when no chunks have been received yet.
-- IsCompleted: TINYINT(1) acts as boolean.
-- =============================================================
CREATE TABLE IF NOT EXISTS FileTransferStates (
    Id              INT             NOT NULL AUTO_INCREMENT,
    FileId          VARCHAR(255)    NOT NULL,
    FileName        VARCHAR(255)    NOT NULL,
    TotalBytes      BIGINT          NOT NULL,
    BytesReceived   BIGINT          NOT NULL,
    LastChunkIndex  INT             NOT NULL DEFAULT -1,
    IsCompleted     TINYINT(1)      NOT NULL DEFAULT 0,
    UpdatedAt       DATETIME        NOT NULL,
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- TABLE: TransferHistories
-- Audit log of all file transfers (Upload, Download, DownloadShared).
-- TransferType: 'Upload', 'Download', 'DownloadShared'.
-- Status: currently always 'Success'.
-- =============================================================
CREATE TABLE IF NOT EXISTS TransferHistories (
    Id              INT             NOT NULL AUTO_INCREMENT,
    Username        VARCHAR(255)    NOT NULL,
    FileName        VARCHAR(255)    NOT NULL,
    FileSize        BIGINT          NOT NULL,
    TransferType    VARCHAR(50)     NOT NULL,
    Status          VARCHAR(50)     NOT NULL,
    CreatedAt       DATETIME        NOT NULL,
    PRIMARY KEY (Id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- TABLE: SharedFiles
-- Stores share codes for file sharing between users.
-- ShareCode: 8-character uppercase GUID.
-- IsActive: TINYINT(1) acts as boolean (always 1 for now).
-- =============================================================
CREATE TABLE IF NOT EXISTS SharedFiles (
    Id              INT             NOT NULL AUTO_INCREMENT,
    OwnerUsername   VARCHAR(255)    NOT NULL,
    FileName        VARCHAR(255)    NOT NULL,
    ShareCode       VARCHAR(50)     NOT NULL,
    AllowedUsername VARCHAR(255)    NOT NULL,
    CreatedAt       DATETIME        NOT NULL,
    IsActive        TINYINT(1)      NOT NULL DEFAULT 1,
    PRIMARY KEY (Id),
    CONSTRAINT UQ_SharedFiles_ShareCode UNIQUE (ShareCode)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================================
-- Indexes for performance
-- =============================================================
CREATE INDEX IX_ClientSessions_UserId ON ClientSessions (UserId);
CREATE INDEX IX_TransferHistories_Username ON TransferHistories (Username);
CREATE INDEX IX_FileTransferStates_FileId ON FileTransferStates (FileId);
CREATE INDEX IX_SharedFiles_ShareCode ON SharedFiles (ShareCode);