-- ==========================================
-- Quick UserTrips Migration (Keep Users & Trips Data)
-- ==========================================

USE [Project];
GO

-- Step 1: Delete only UserTrips data
PRINT 'Deleting UserTrips data...';
DELETE FROM dbo.UserTrips;
PRINT '✓ UserTrips data cleared';
GO

-- Step 2: Drop UserTrips constraints
PRINT 'Dropping UserTrips constraints...';

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserTrips_Users_user_email')
BEGIN
    ALTER TABLE dbo.UserTrips DROP CONSTRAINT FK_UserTrips_Users_user_email;
    PRINT '✓ Dropped FK_UserTrips_Users_user_email';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_UserTrips_User_Trip')
BEGIN
    ALTER TABLE dbo.UserTrips DROP CONSTRAINT UQ_UserTrips_User_Trip;
    PRINT '✓ Dropped UQ_UserTrips_User_Trip';
END
GO

-- Step 3: Drop old user_email column
PRINT 'Dropping user_email column...';
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserTrips') AND name = 'user_email')
BEGIN
    ALTER TABLE dbo.UserTrips DROP COLUMN user_email;
    PRINT '✓ Dropped user_email column';
END
GO

-- Step 4: Add Id column to Users table (if not exists)
PRINT 'Adding Id column to Users...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'Id')
BEGIN
    -- Drop existing PK on email first
    IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_Users')
    BEGIN
        ALTER TABLE dbo.Users DROP CONSTRAINT PK_Users;
        PRINT '✓ Dropped old PK on email';
    END
    
    -- Add Id column
    ALTER TABLE dbo.Users ADD Id INT IDENTITY(1,1) NOT NULL;
    PRINT '✓ Added Id column';
    
    -- Create new PK on Id
    ALTER TABLE dbo.Users ADD CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id);
    PRINT '✓ Created PK on Id';
    
    -- Make email unique
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_Users_Email')
    BEGIN
        ALTER TABLE dbo.Users ADD CONSTRAINT UQ_Users_Email UNIQUE ([email]);
        PRINT '✓ Added UNIQUE constraint on email';
    END
    
    -- Ensure email is NOT NULL
    ALTER TABLE dbo.Users ALTER COLUMN [email] NVARCHAR(450) NOT NULL;
    PRINT '✓ Email set to NOT NULL';
END
ELSE
BEGIN
    PRINT '✓ Id column already exists';
END
GO

-- Step 5: Add UserId column to UserTrips
PRINT 'Adding UserId column to UserTrips...';
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.UserTrips') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.UserTrips ADD UserId INT NOT NULL DEFAULT(0);
    PRINT '✓ Added UserId column';
END
GO

-- Step 6: Create new foreign key
PRINT 'Creating new foreign key...';
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_UserTrips_Users_UserId')
BEGIN
    ALTER TABLE dbo.UserTrips 
    ADD CONSTRAINT FK_UserTrips_Users_UserId 
    FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) 
    ON DELETE CASCADE;
    PRINT '✓ Created FK_UserTrips_Users_UserId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_UserTrips_User_Trip')
BEGIN
    ALTER TABLE dbo.UserTrips 
    ADD CONSTRAINT UQ_UserTrips_User_Trip UNIQUE (UserId, TripID);
    PRINT '✓ Created UQ_UserTrips_User_Trip';
END
GO

-- Step 7: Remove DEFAULT constraint from UserId
PRINT 'Cleaning up...';
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
    PRINT '✓ Removed DEFAULT constraint';
END
GO

PRINT '';
PRINT '==========================================';
PRINT 'Migration completed!';
PRINT 'Users data: PRESERVED';
PRINT 'Trips data: PRESERVED';
PRINT 'UserTrips data: DELETED';
PRINT '==========================================';
GO

-- Verify
SELECT 'Users Table' AS TableName, COUNT(*) AS RecordCount FROM dbo.Users
UNION ALL
SELECT 'Trips Table', COUNT(*) FROM dbo.Trips
UNION ALL
SELECT 'UserTrips Table', COUNT(*) FROM dbo.UserTrips;
GO
