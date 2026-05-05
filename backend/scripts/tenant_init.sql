-- Tenant Database Initialization Template
-- This script should be run for each new tenant financial year database
-- Note: RefreshTokens table does NOT exist in tenant DBs (they're in master DB)

-- Replace {DATABASE_NAME} with actual tenant FY database name (e.g., smreader_tenant1_fy2024_25)
CREATE DATABASE IF NOT EXISTS {DATABASE_NAME};
USE {DATABASE_NAME};

CREATE TABLE IF NOT EXISTS Roles (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(50) NOT NULL UNIQUE,
    Description VARCHAR(255),
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt   DATETIME NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed Default Roles
INSERT INTO Roles (Name, Description) VALUES ('Admin', 'System administrator')
ON DUPLICATE KEY UPDATE Description = VALUES(Description);

INSERT INTO Roles (Name, Description) VALUES ('User', 'Standard user')
ON DUPLICATE KEY UPDATE Description = VALUES(Description);
