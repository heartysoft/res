﻿CREATE TYPE [dbo].[EventParameter] AS TABLE(
	[EventId] [uniqueidentifier] NOT NULL,
	[StreamId] [nvarchar](256) NOT NULL,
	[ContextName] [nvarchar](50) NOT NULL,
	[Sequence] [bigint] NULL,
	[SequenceInCommit] [int],
	[TimeStamp] [datetime2](4) NULL,
	[EventType] [nvarchar](500) NULL,
    [Headers] [nvarchar](max) NULL,
	[Body] [nvarchar](max) NULL,
	[CommitId] [uniqueidentifier] NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[EventId] ASC,
	[StreamId] ASC,
	[ContextName] ASC,
	[CommitId] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO