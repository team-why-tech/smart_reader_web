-- Migration Script: Single Database → Multi-Tenant Architecture
-- This script migrates the existing SmreaderDB to the new multi-tenant structure
-- Run this script carefully in the correct order

-- ====================
-- STEP 1: Create Master Database
-- ====================

SOURCE master_init.sql;

-- ====================
-- STEP 2: Create First Tenant's FY Database
-- ====================

CREATE DATABASE IF NOT EXISTS smreader_tenant1_fy2024_25;
USE smreader_tenant1_fy2024_25;

-- Copy table structure and data from SmreaderDB (excluding RefreshTokens)
CREATE TABLE IF NOT EXISTS Roles LIKE SmreaderDB.Roles;
INSERT INTO Roles SELECT * FROM SmreaderDB.Roles;

CREATE TABLE IF NOT EXISTS Users LIKE SmreaderDB.Users;
INSERT INTO Users SELECT * FROM SmreaderDB.Users;

CREATE TABLE IF NOT EXISTS AuditLogs LIKE SmreaderDB.AuditLogs;
INSERT INTO AuditLogs SELECT * FROM SmreaderDB.AuditLogs;

-- Note: RefreshTokens are NOT copied to tenant DB (they go to master DB)

-- ====================
-- STEP 3: Migrate RefreshTokens to Master DB
-- ====================

USE SmreaderMasterDB;

-- Migrate existing refresh tokens from SmreaderDB to master DB
-- Assumes TenantId=1 for the default tenant
INSERT INTO refresh_tokens (UserId, TenantId, Token, ExpiresAt, CreatedAt, RevokedAt, IsRevoked)
SELECT 
    UserId,
    1 AS TenantId,  -- Default tenant ID
    Token,
    ExpiresAt,
    CreatedAt,
    RevokedAt,
    IsRevoked
FROM SmreaderDB.RefreshTokens
WHERE IsRevoked = 0 AND ExpiresAt > NOW();

-- ====================
-- STEP 4: Update Master DB with Tenant Configuration
-- ====================

-- Tenant record should already exist from master_init.sql seed data
-- Update tenant_databases with actual connection string
UPDATE tenant_databases 
SET ConnectionString = 'Server=localhost;Port=3306;Database=smreader_tenant1_fy2024_25;Uid=root;Pwd=yourpassword;'
WHERE TenantId = 1 AND FinancialYear = '2024-25';

-- ====================
-- STEP 5: Verification Queries
-- ====================

-- Verify tenant setup
USE SmreaderMasterDB;
SELECT * FROM tenants;
SELECT * FROM tenant_databases;
SELECT COUNT(*) AS ActiveRefreshTokens FROM refresh_tokens WHERE IsRevoked = 0;

-- Verify tenant data
USE smreader_tenant1_fy2024_25;
SELECT COUNT(*) AS TotalUsers FROM Users;
SELECT COUNT(*) AS TotalRoles FROM Roles;
SELECT COUNT(*) AS TotalAuditLogs FROM AuditLogs;

-- ====================
-- STEP 6: OPTIONAL - Backup and Drop Old Database
-- ====================

-- IMPORTANT: Only run this after verifying the migration was successful!
-- Create a backup first: mysqldump -u root -p SmreaderDB > SmreaderDB_backup.sql

-- DROP DATABASE SmreaderDB;  -- UNCOMMENT ONLY AFTER SUCCESSFUL MIGRATION AND BACKUP

-- ====================
-- Migration Complete
-- ====================

-- Next steps:
-- 1. Update appsettings.json with MasterConnection
-- 2. Restart the API application
-- 3. Test login with tenantCode: "default"
-- 4. Verify JWT contains tenant_id and fy claims
-- 5. Test protected endpoints to ensure tenant DB is being used
