CREATE TABLE [dbo].[QueueAllocations]
(
	[QueueId] nvarchar(50) NOT NULL ,
	[SubscriberId] nvarchar(50) NOT NULL,
	[ExpiresAt] datetime2(4) NOT NULL,
	[StartMarker] bigint NOT NULL,
	[EndMarker] bigint NOT NULL, 
    PRIMARY KEY ([QueueId], [SubscriberId])
)
