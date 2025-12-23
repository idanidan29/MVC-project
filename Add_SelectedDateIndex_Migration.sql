-- Migration Script: Add SelectedDateIndex to UserTrips table
-- Purpose: Track which date option (main or variation) user selected when adding to cart
-- Date: 2025-12-23
-- Database: Project

USE [Project];
GO

-- Check if column already exists
IF NOT EXISTS (
    SELECT 1 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = 'dbo' 
    AND TABLE_NAME = 'UserTrips' 
    AND COLUMN_NAME = 'SelectedDateIndex'
)
BEGIN
    PRINT 'Adding SelectedDateIndex column to UserTrips table...';
    
    -- Add the SelectedDateIndex column
    -- -1 = main trip date (default)
    -- 0, 1, 2, ... = index of date variation in TripDates table
    ALTER TABLE dbo.UserTrips
    ADD SelectedDateIndex INT NOT NULL DEFAULT -1;
    
    PRINT 'SelectedDateIndex column added successfully!';
END
ELSE
BEGIN
    PRINT 'SelectedDateIndex column already exists. No changes made.';
END
GO

-- Verify the change
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo'
AND TABLE_NAME = 'UserTrips'
AND COLUMN_NAME = 'SelectedDateIndex';
GO

PRINT 'Migration completed!';
