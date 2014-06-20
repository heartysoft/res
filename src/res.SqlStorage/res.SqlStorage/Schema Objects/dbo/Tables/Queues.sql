CREATE TABLE [dbo].[Queues]
(
	[QueueId] NVARCHAR(50) NOT NULL , 
    [Context] NVARCHAR(50) NOT NULL, 
    [Filter] NVARCHAR(50) NOT NULL,
	[NextMarker] BIGINT NOT NULL, 
    PRIMARY KEY ([QueueId])
)

GO

CREATE INDEX [IX_Queue_Context_Filter] ON [dbo].[Queues] ([Context], [Filter])
