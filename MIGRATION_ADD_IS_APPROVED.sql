-- Migration script to add is_approved column to members table
-- This column tracks whether a member has been approved by Admin
-- When a Captain adds a member, is_approved = false
-- When an Admin adds a member, is_approved = true
-- Admin can approve members added by Captains using the approve endpoint

-- For MySQL/MariaDB
ALTER TABLE `members` 
ADD COLUMN `is_approved` TINYINT(1) NOT NULL DEFAULT 1 
AFTER `is_active`;

-- For SQL Server
-- ALTER TABLE [dbo].[members]
-- ADD [is_approved] BIT NOT NULL DEFAULT 1;

-- For SQLite
-- ALTER TABLE members 
-- ADD COLUMN is_approved INTEGER NOT NULL DEFAULT 1;

-- Update existing records to be approved by default
UPDATE `members` SET `is_approved` = 1 WHERE `is_approved` IS NULL OR `is_approved` = 0;

