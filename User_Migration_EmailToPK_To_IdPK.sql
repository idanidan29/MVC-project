-- ==========================================
-- User Migration: Email PK → Id PK
-- Date: 2024
-- Description: Refactor Users table to use auto-incrementing Id as Primary Key
--              instead of Email. Update all foreign key relationships.
-- ==========================================

USE [Project];
GO

-- ==========================================
-- STEP 1: Clear all data from tables
-- ==========================================
PRINT 'Step 1: Clearing all data from tables...';

-- Delete from dependent tables first (respect FK constraints)
DELETE FROM dbo.TripDates;
DELETE FROM dbo.UserTrips;
DELETE FROM dbo.TripImages;
DELETE FROM dbo.Trips;
DELETE FROM dbo.Users;

PRINT '✓ All data cleared';
GO

-- ==========================================
-- STEP 2: Drop existing constraints on UserTrips
-- ==========================================
PRINT 'Step 2: Dropping UserTrips constraints...';

-- Drop the foreign key constraint from UserTrips to Users (based on email)
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserTrips_Users_user_email')
BEGIN
    ALTER TABLE dbo.UserTrips DROP CONSTRAINT FK_UserTrips_Users_user_email;
    PRINT '✓ Dropped FK_UserTrips_Users_user_email';
END

-- Drop the unique constraint on UserTrips
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_UserTrips_User_Trip')
BEGIN
    ALTER TABLE dbo.UserTrips DROP CONSTRAINT UQ_UserTrips_User_Trip;
    PRINT '✓ Dropped UQ_UserTrips_User_Trip';
END
GO

-- ==========================================
-- STEP 3: Drop old UserTrips.user_email column
-- ==========================================
PRINT 'Step 3: Dropping old user_email column from UserTrips...';

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserTrips') AND name = 'user_email')
BEGIN
    ALTER TABLE dbo.UserTrips DROP COLUMN user_email;
    PRINT '✓ Dropped user_email column from UserTrips';
END
GO

-- ==========================================
-- STEP 4: Modify Users table - Add Id column and update PK
-- ==========================================
PRINT 'Step 4: Modifying Users table...';

-- Drop the existing primary key constraint on email
IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_Users' AND parent_object_id = OBJECT_ID('dbo.Users'))
BEGIN
    ALTER TABLE dbo.Users DROP CONSTRAINT PK_Users;
    PRINT '✓ Dropped old PK constraint on email';
END

-- Add the new Id column as IDENTITY
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Id')
BEGIN
    ALTER TABLE dbo.Users ADD Id INT IDENTITY(1,1) NOT NULL;
    PRINT '✓ Added Id column as IDENTITY(1,1)';
END

-- Create new primary key on Id
ALTER TABLE dbo.Users ADD CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id);
PRINT '✓ Created new PK on Id';

-- Make email column UNIQUE and NOT NULL (if not already)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_Email' AND object_id = OBJECT_ID('dbo.Users'))
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT UQ_Users_Email UNIQUE ([email]);
    PRINT '✓ Added UNIQUE constraint on email';
END

-- Ensure email is NOT NULL
ALTER TABLE dbo.Users ALTER COLUMN [email] NVARCHAR(450) NOT NULL;
PRINT '✓ Email column set to NOT NULL';
GO

-- ==========================================
-- STEP 5: Add UserId column to UserTrips
-- ==========================================
PRINT 'Step 5: Adding UserId column to UserTrips...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserTrips') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.UserTrips ADD UserId INT NOT NULL DEFAULT(0);
    PRINT '✓ Added UserId column to UserTrips';
END
GO

-- ==========================================
-- STEP 6: Create new foreign key relationship
-- ==========================================
PRINT 'Step 6: Creating new foreign key constraints...';

-- Add foreign key from UserTrips.UserId to Users.Id
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserTrips_Users_UserId')
BEGIN
    ALTER TABLE dbo.UserTrips 
    ADD CONSTRAINT FK_UserTrips_Users_UserId 
    FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) 
    ON DELETE CASCADE;
    PRINT '✓ Created FK_UserTrips_Users_UserId with CASCADE DELETE';
END

-- Add unique constraint on (UserId, TripID)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_UserTrips_User_Trip')
BEGIN
    ALTER TABLE dbo.UserTrips 
    ADD CONSTRAINT UQ_UserTrips_User_Trip UNIQUE (UserId, TripID);
    PRINT '✓ Created unique constraint UQ_UserTrips_User_Trip';
END
GO

-- ==========================================
-- STEP 7: Remove DEFAULT constraint from UserId
-- ==========================================
PRINT 'Step 7: Cleaning up temporary constraints...';

DECLARE @ConstraintName NVARCHAR(200);
SELECT @ConstraintName = name 
FROM sys.default_constraints 
WHERE parent_object_id = OBJECT_ID('dbo.UserTrips') 
AND parent_column_id = (SELECT column_id FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserTrips') AND name = 'UserId');

IF @ConstraintName IS NOT NULL
BEGIN
    DECLARE @SQL NVARCHAR(500);
    SET @SQL = 'ALTER TABLE dbo.UserTrips DROP CONSTRAINT ' + @ConstraintName;
    EXEC sp_executesql @SQL;
    PRINT '✓ Removed DEFAULT constraint from UserId';
END
GO

-- ==========================================
-- VERIFICATION
-- ==========================================
PRINT '';
PRINT '==========================================';
PRINT 'Migration completed successfully!';
PRINT '==========================================';
PRINT '';
PRINT 'Users table structure:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    c.is_identity AS IsIdentity
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Users')
ORDER BY c.column_id;

PRINT '';
PRINT 'UserTrips table structure:';
SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.UserTrips')
ORDER BY c.column_id;

PRINT '';
PRINT 'Foreign Keys:';
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fkc.parent_object_id, fkc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ReferencedColumn
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.UserTrips');

PRINT '';
PRINT '==========================================';
PRINT 'IMPORTANT: Update your C# models and EF configuration!';
PRINT '==========================================';
GO
