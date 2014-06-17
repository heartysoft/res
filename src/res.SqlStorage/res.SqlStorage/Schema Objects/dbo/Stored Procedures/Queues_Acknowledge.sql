CREATE PROCEDURE [dbo].[Queues_Acknowledge]
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@StartMarker bigint,
	@EndMarker bigint,
	@Count int,
	@AllocationTimeInMilliseconds int
AS
	Declare @Now datetime2(4) = GetUtcDate()
	BEGIN TRAN
		EXEC Queues_Acknowledge_Acknowledge @QueueId, @SubscriberId, @StartMarker, @EndMarker
		Exec Queues_Subscribe_Allocate @QueueId, @SubscriberId, @Count, @AllocationTimeInMilliseconds, @Now
		Exec Queues_Subscribe_FetchEvents @QueueId, @SubscriberId
	COMMIT
RETURN 0


