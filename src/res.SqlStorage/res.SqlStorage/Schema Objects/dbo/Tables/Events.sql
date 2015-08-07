CREATE TABLE [dbo].[Events](
	[EventId] [uniqueidentifier] NOT NULL,
	[StreamId] [nvarchar](256) NOT NULL,
	[ContextName] [nvarchar](50) NOT NULL,
	[Sequence] [bigint] NOT NULL,
	[GlobalSequence] BIGINT NOT NULL IDENTITY(-9223372036854775808, 1),
	[TimeStamp] [datetime2](4) NOT NULL,
	[EventType] [nvarchar](500) NOT NULL,
    [Headers] [nvarchar](max) NULL,
	[Body] [nvarchar](max) NOT NULL, 
    CONSTRAINT [PK_Events] PRIMARY KEY ([GlobalSequence])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE NONCLUSTERED INDEX [IX_TimeStamp] ON [dbo].[Events]
(
	[TimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Events] ON [dbo].[Events]
(
	[ContextName] ASC,
	[StreamId] ASC,
	[Sequence] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO
