USE [project]
GO

/* Add cancellation deadline support to dbo.Trips
   - Adds nullable [CancellationEndDate]
   - Adds computed, persisted [EffectiveCancellationEndDate] that falls back to 7 days before [StartDate] when no explicit date is set
   - Enforces [EffectiveCancellationEndDate] <= [StartDate]

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/* 1) Add nullable CancellationEndDate if missing */
IF COL_LENGTH('dbo.Trips', 'CancellationEndDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Trips]
        ADD [CancellationEndDate] [date] NULL;
END
GO

/* 2) Add computed EffectiveCancellationEndDate (fallback to StartDate - 7 days) if missing */
IF COL_LENGTH('dbo.Trips', 'EffectiveCancellationEndDate') IS NULL
BEGIN
    ALTER TABLE [dbo].[Trips]
        ADD [EffectiveCancellationEndDate] AS (ISNULL([CancellationEndDate], DATEADD(day, -7, [StartDate]))) PERSISTED;
END
GO

/* 3) Add a check constraint to ensure cancellation deadline is not after trip start */
IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints c
    WHERE c.name = 'CHK_Trips_CancellationDeadline'
      AND c.parent_object_id = OBJECT_ID('dbo.Trips')
)
BEGIN
    ALTER TABLE [dbo].[Trips] WITH CHECK ADD CONSTRAINT [CHK_Trips_CancellationDeadline]
        CHECK ([EffectiveCancellationEndDate] <= [StartDate]);
    ALTER TABLE [dbo].[Trips] CHECK CONSTRAINT [CHK_Trips_CancellationDeadline];
END
GO

/* Optional: quickly preview resulting schema */
-- SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
-- FROM INFORMATION_SCHEMA.COLUMNS
-- WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Trips'
-- ORDER BY ORDINAL_POSITION;
