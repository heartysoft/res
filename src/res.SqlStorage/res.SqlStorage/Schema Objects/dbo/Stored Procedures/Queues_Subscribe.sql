CREATE PROCEDURE [dbo].[Queues_Subscribe]
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@Context nvarchar(50),
	@Filter nvarchar(200),
	@StartTime datetime2(4),
	@Count int,
	@AllocationTimeInMilliseconds int
AS
	BEGIN TRAN
		Declare @Now datetime2(4) = GetUtcDate()
		Exec Queues_Subscribe_CreateIfNotExists @QueueId, @Context, @Filter, @StartTime
		Exec Queues_Subscribe_Allocate @QueueId, @SubscriberId, @Count, @AllocationTimeInMilliseconds, @Now
		Exec Queues_Subscribe_FetchEvents @QueueId, @SubscriberId
	COMMIT
RETURN 0
