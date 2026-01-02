-- Add ExpiresAt column to UserTrips table for cart expiration
-- This allows automatic removal of cart items after specified time

USE [project];
GO

-- Check if column already exists
IF NOT EXISTS (SELECT * FROM sys.columns 
               WHERE object_id = OBJECT_ID('UserTrips') 
               AND name = 'ExpiresAt')
BEGIN
    -- Add ExpiresAt column
    ALTER TABLE [dbo].[UserTrips]
    ADD [ExpiresAt] DATETIME2 NULL;
    
    PRINT 'ExpiresAt column added to UserTrips table successfully.';
END
ELSE
BEGIN
    PRINT 'ExpiresAt column already exists in UserTrips table.';
END
GO

-- Create index for faster expiration queries
IF NOT EXISTS (SELECT * FROM sys.indexes 
               WHERE name = 'IX_UserTrips_ExpiresAt' 
               AND object_id = OBJECT_ID('UserTrips'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_UserTrips_ExpiresAt] 
    ON [dbo].[UserTrips] ([ExpiresAt])
    WHERE [ExpiresAt] IS NOT NULL;
    
    PRINT 'Index IX_UserTrips_ExpiresAt created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_UserTrips_ExpiresAt already exists.';
END
GO

PRINT 'UserTrips cart expiration migration completed.';
