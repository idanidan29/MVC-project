USE [project]
GO

/****** Object:  Table [dbo].[UserFeedback]    Script Date: 10/01/2026 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserFeedback](
	[FeedbackID] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Rating] [int] NOT NULL,
	[FeedbackText] [nvarchar](1000) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[IsApproved] [bit] NOT NULL,
	[IsFeatured] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FeedbackID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[UserFeedback] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
GO

ALTER TABLE [dbo].[UserFeedback] ADD DEFAULT ((0)) FOR [IsApproved]
GO

ALTER TABLE [dbo].[UserFeedback] ADD DEFAULT ((0)) FOR [IsFeatured]
GO

ALTER TABLE [dbo].[UserFeedback]  WITH CHECK ADD  CONSTRAINT [FK_UserFeedback_Users] FOREIGN KEY([UserId])
REFERENCES [dbo].[Users] ([Id])
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[UserFeedback] CHECK CONSTRAINT [FK_UserFeedback_Users]
GO

ALTER TABLE [dbo].[UserFeedback]  WITH CHECK ADD CHECK  (([Rating]>=(1) AND [Rating]<=(5)))
GO

PRINT 'UserFeedback table created successfully!';
