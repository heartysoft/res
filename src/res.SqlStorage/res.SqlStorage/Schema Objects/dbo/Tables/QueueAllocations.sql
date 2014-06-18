CREATE TABLE [dbo].[QueueAllocations]
(
	[AllocationId] BIGINT NOT NULL IDENTITY(-9223372036854775808, 1),
	[QueueId] nvarchar(50) NOT NULL ,
	[SubscriberId] nvarchar(50) NOT NULL,
	[ExpiresAt] datetime2(4) NOT NULL,
	[StartMarker] bigint NOT NULL,
	[EndMarker] bigint NOT NULL, 
    CONSTRAINT [PK_QueueAllocations] PRIMARY KEY ([AllocationId])
)

GO

CREATE INDEX [IX_QueueAllocations_QueueId_SubscriberId] ON [dbo].[QueueAllocations] ([QueueId], [SubscriberId])
