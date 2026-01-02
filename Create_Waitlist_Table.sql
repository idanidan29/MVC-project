-- Create Waitlist table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Waitlist' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE [dbo].[Waitlist] (
        [WaitlistID] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [TripId] INT NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT 'Waiting',
        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
        [ExpiresAt] DATETIME NULL,
        [EmailSentAt] DATETIME NULL,
        
        CONSTRAINT [PK_Waitlist] PRIMARY KEY CLUSTERED ([WaitlistID] ASC),
        
        CONSTRAINT [FK_Waitlist_Users] FOREIGN KEY ([UserId]) 
            REFERENCES [dbo].[Users] ([Id]) 
            ON DELETE CASCADE,
        
        CONSTRAINT [FK_Waitlist_Trips] FOREIGN KEY ([TripId]) 
            REFERENCES [dbo].[Trips] ([TripID]) 
            ON DELETE CASCADE,
        
        CONSTRAINT [CK_Waitlist_Status] CHECK ([Status] IN ('Waiting', 'Notified', 'Booked', 'Expired'))
    );

    -- Create index for faster lookups
    CREATE NONCLUSTERED INDEX [IX_Waitlist_UserId_TripId] ON [dbo].[Waitlist] ([UserId], [TripId]);
    CREATE NONCLUSTERED INDEX [IX_Waitlist_Status] ON [dbo].[Waitlist] ([Status]);
    CREATE NONCLUSTERED INDEX [IX_Waitlist_TripId_CreatedAt] ON [dbo].[Waitlist] ([TripId], [CreatedAt]);

    PRINT 'Waitlist table created successfully.';
END
ELSE
BEGIN
    PRINT 'Waitlist table already exists.';
END
GO
