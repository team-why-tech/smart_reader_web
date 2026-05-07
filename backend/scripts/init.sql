-- ============================================================================
-- SmreaderAPI Multi-Tenant Database Schema
-- ============================================================================

-- ============================================================================
-- 1. MASTER DATABASE — Tenant Registry + Refresh Tokens
-- ============================================================================
-- Connection: appsettings.json -> ConnectionStrings:DefaultConnection

-- CREATE DATABASE IF NOT EXISTS smreader_master;
-- USE smreader_master;

-- ca_management — tenant registry (assumed to already exist)
-- CREATE TABLE IF NOT EXISTS `ca_management` (
--   `id` int(11) NOT NULL AUTO_INCREMENT,
--   `name` varchar(255) NOT NULL,
--   `company_name` varchar(255) NOT NULL,
--   `db_name` varchar(50) DEFAULT NULL,
--   `db_user` varchar(50) DEFAULT NULL,
--   `db_pwd` varchar(250) DEFAULT NULL,
--   `date_from` date DEFAULT '1900-01-01',
--   `date_to` date DEFAULT '1900-01-01',
--   PRIMARY KEY (`id`)
-- ) ENGINE=InnoDB DEFAULT CHARSET=latin1;

-- ca_refresh_tokens — JWT refresh tokens (in master DB, scoped by tenant_id)
CREATE TABLE IF NOT EXISTS `ca_refresh_tokens` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `tenant_id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `token` varchar(512) NOT NULL,
  `expires_at` datetime NOT NULL,
  `created_at` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `revoked_at` datetime DEFAULT NULL,
  `is_revoked` tinyint(1) NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`),
  INDEX `IX_RefreshTokens_Token` (`token`),
  INDEX `IX_RefreshTokens_Tenant_User` (`tenant_id`, `user_id`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;


-- ============================================================================
-- 2. TENANT DATABASE — Run in each tenant database
-- ============================================================================
-- ca_users table already exists in tenant DBs.
-- Only AuditLogs needs to be created.

CREATE TABLE IF NOT EXISTS `AuditLogs` (
  `Id` int(11) NOT NULL AUTO_INCREMENT,
  `UserId` int(11) DEFAULT NULL,
  `Action` varchar(100) NOT NULL,
  `EntityName` varchar(100) NOT NULL,
  `EntityId` int(11) DEFAULT NULL,
  `Timestamp` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Details` text,
  `CreatedAt` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `UpdatedAt` datetime DEFAULT NULL,
  PRIMARY KEY (`Id`),
  INDEX `IX_AuditLogs_UserId` (`UserId`),
  INDEX `IX_AuditLogs_Entity` (`EntityName`, `EntityId`),
  CONSTRAINT `FK_AuditLogs_Users` FOREIGN KEY (`UserId`) REFERENCES `ca_users`(`id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1;
