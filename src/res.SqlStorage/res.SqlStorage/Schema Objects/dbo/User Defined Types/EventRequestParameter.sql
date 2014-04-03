CREATE TYPE [dbo].[EventRequestParameter] AS TABLE(
	[EventId] [uniqueidentifier] NOT NULL,
	[StreamId] [nvarchar](50) NOT NULL,
	[ContextName] [nvarchar](50) NOT NULL,
	[RequestId] [uniqueidentifier] NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[EventId] ASC,
	[StreamId] ASC,
	[ContextName] ASC,
	[RequestId] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO