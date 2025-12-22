-- SQL Script to Create TripDates Table
-- This table stores multiple date variations for each trip

CREATE TABLE dbo.TripDates (
    TripDateID INT IDENTITY(1,1) NOT NULL,
    TripID INT NOT NULL,
    StartDate DATETIME NOT NULL,
    EndDate DATETIME NOT NULL,
    AvailableRooms INT NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_TripDates PRIMARY KEY (TripDateID),
    CONSTRAINT FK_TripDates_Trips FOREIGN KEY (TripID) 
        REFERENCES dbo.Trips(TripID) ON DELETE CASCADE
);

-- Create index for better query performance
CREATE INDEX IX_TripDates_TripID ON dbo.TripDates(TripID);

-- Comments:
-- TripDateID: Primary key, auto-incrementing
-- TripID: Foreign key to Trips table
-- StartDate: Start date of this date variation
-- EndDate: End date of this date variation  
-- AvailableRooms: Number of rooms available for this specific date range
-- CreatedAt: Timestamp when this date variation was added
