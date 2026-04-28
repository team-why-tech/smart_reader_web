CREATE DATABASE IF NOT EXISTS SmreaderDB;
USE SmreaderDB;

CREATE TABLE IF NOT EXISTS Roles (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt   DATETIME NULL
);

CREATE TABLE IF NOT EXISTS Users (
    Id           INT AUTO_INCREMENT PRIMARY KEY,
    Name         VARCHAR(100) NOT NULL,
    Email        VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId       INT NOT NULL,
    CreatedAt    DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt    DATETIME NULL,
    IsActive     TINYINT(1) NOT NULL DEFAULT 1,
    UNIQUE INDEX IX_Users_Email (Email),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

CREATE TABLE IF NOT EXISTS RefreshTokens (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    UserId     INT NOT NULL,
    Token      VARCHAR(512) NOT NULL,
    ExpiresAt  DATETIME NOT NULL,
    CreatedAt  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RevokedAt  DATETIME NULL,
    IsRevoked  TINYINT(1) NOT NULL DEFAULT 0,
    UpdatedAt  DATETIME NULL,
    INDEX IX_RefreshTokens_Token (Token),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AuditLogs (
    Id         INT AUTO_INCREMENT PRIMARY KEY,
    UserId     INT NULL,
    Action     VARCHAR(100) NOT NULL,
    EntityName VARCHAR(100) NOT NULL,
    EntityId   INT NULL,
    Timestamp  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Details    TEXT NULL,
    CreatedAt  DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt  DATETIME NULL,
    INDEX IX_AuditLogs_UserId (UserId),
    INDEX IX_AuditLogs_Entity (EntityName, EntityId),
    CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL
);

-- Seed Data
INSERT INTO Roles (Name, Description) VALUES ('Admin', 'System administrator')
ON DUPLICATE KEY UPDATE Description = VALUES(Description);

INSERT INTO Roles (Name, Description) VALUES ('User', 'Standard user')
ON DUPLICATE KEY UPDATE Description = VALUES(Description);
