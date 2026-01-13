-- Migration: Alter created_by column from INT to VARCHAR to store captain's name
-- This allows storing the full name of the captain who created the member
-- Run this migration if your created_by column is currently INT type

-- For MySQL/MariaDB
ALTER TABLE `members` 
MODIFY COLUMN `created_by` VARCHAR(255) NULL;

-- For SQL Server
-- ALTER TABLE [dbo].[members]
-- ALTER COLUMN [created_by] NVARCHAR(255) NULL;

-- For SQLite
-- Note: SQLite doesn't support ALTER COLUMN directly
-- You would need to recreate the table or use a workaround
