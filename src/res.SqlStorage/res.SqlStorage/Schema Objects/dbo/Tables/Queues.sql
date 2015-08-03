CREATE TABLE [dbo].[Queues]
(
    [Context] NVARCHAR(50) NOT NULL, 
	[QueueId] NVARCHAR(50) NOT NULL , 
    [Filter] NVARCHAR(256) NOT NULL,
	[NextMarker] BIGINT NOT NULL, 
    PRIMARY KEY ([Context], [QueueId])
)

GO

CREATE INDEX [IX_Queue_Context_Filter] ON [dbo].[Queues] ([Context], [Filter])
