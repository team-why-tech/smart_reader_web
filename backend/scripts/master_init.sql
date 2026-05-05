-- Master Database Initialization Script
-- This database acts as the control plane for multi-tenant architecture

CREATE DATABASE IF NOT EXISTS SmreaderMasterDB;
USE SmreaderMasterDB;

-- Tenants table
CREATE TABLE IF NOT EXISTS tenants (
    Id          INT AUTO_INCREMENT PRIMARY KEY,
    Name        VARCHAR(100) NOT NULL,
    Code        VARCHAR(50) NOT NULL UNIQUE,
    IsActive    TINYINT(1) NOT NULL DEFAULT 1,
    CreatedAt   DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt   DATETIME NULL,
    INDEX IX_Tenants_Code (Code),
    INDEX IX_Tenants_IsActive (IsActive)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tenant Databases table (maps tenant + FY to connection string)
CREATE TABLE IF NOT EXISTS tenant_databases (
    Id               INT AUTO_INCREMENT PRIMARY KEY,
    TenantId         INT NOT NULL,
    FinancialYear    VARCHAR(10) NOT NULL COMMENT 'Format: YYYY-YY e.g., 2024-25',
    ConnectionString VARCHAR(512) NOT NULL,
    IsDefault        TINYINT(1) NOT NULL DEFAULT 0,
    CreatedAt        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt        DATETIME NULL,
    CONSTRAINT FK_TenantDatabases_Tenants FOREIGN KEY (TenantId) REFERENCES tenants(Id) ON DELETE CASCADE,
    UNIQUE INDEX UX_TenantDatabases_TenantFY (TenantId, FinancialYear),
    INDEX IX_TenantDatabases_IsDefault (TenantId, IsDefault)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Refresh Tokens table (centralized in master DB)
CREATE TABLE IF NOT EXISTS refresh_tokens (
    Id               INT AUTO_INCREMENT PRIMARY KEY,
    UserId           INT NOT NULL COMMENT 'User ID from tenant database',
    TenantId         INT NOT NULL,
    Token            VARCHAR(512) NOT NULL,
    ExpiresAt        DATETIME NOT NULL,
    CreatedAt        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    RevokedAt        DATETIME NULL,
    IsRevoked        TINYINT(1) NOT NULL DEFAULT 0,
    ReplacedByToken  VARCHAR(512) NULL,
    IpAddress        VARCHAR(45) NULL,
    CONSTRAINT FK_RefreshTokens_Tenants FOREIGN KEY (TenantId) REFERENCES tenants(Id) ON DELETE CASCADE,
    INDEX IX_RefreshTokens_Token (Token),
    INDEX IX_RefreshTokens_UserTenant (UserId, TenantId),
    INDEX IX_RefreshTokens_Expiry (ExpiresAt, IsRevoked)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Seed default tenant
INSERT INTO tenants (Id, Name, Code, IsActive, CreatedAt)
VALUES (1, 'Default Tenant', 'default', 1, CURRENT_TIMESTAMP)
ON DUPLICATE KEY UPDATE Name = VALUES(Name), Code = VALUES(Code);

-- Seed default tenant database connection (update connection string as needed)
INSERT INTO tenant_databases (TenantId, FinancialYear, ConnectionString, IsDefault, CreatedAt)
VALUES (
    1,
    '2024-25',
    'Server=localhost;Port=3306;Database=smreader_tenant1_fy2024_25;Uid=root;Pwd=yourpassword;',
    1,
    CURRENT_TIMESTAMP
)
ON DUPLICATE KEY UPDATE 
    ConnectionString = VALUES(ConnectionString),
    IsDefault = VALUES(IsDefault);
