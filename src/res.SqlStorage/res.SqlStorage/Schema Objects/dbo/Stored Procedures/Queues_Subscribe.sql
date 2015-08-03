CREATE PROCEDURE [dbo].[Queues_Subscribe]
	@Context nvarchar(50),
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@Filter nvarchar(256),
	@StartTime datetime2(4),
	@Count int,
	@AllocationTimeInMilliseconds int
AS
	Declare @AllocationId bigint = NULL
	Exec Queues_Subscribe_CreateIfNotExists @Context, @QueueId, @Filter, @StartTime

	BEGIN TRAN
		Declare @Now datetime2(4) = GetUtcDate()		
	    Exec Queues_Subscribe_Allocate @Context, @QueueId, @SubscriberId, @Count, @AllocationTimeInMilliseconds, @Now, @AllocationId = @AllocationId OUTPUT
		Exec Queues_Subscribe_FetchEvents @AllocationId
		SELECT @AllocationId AS AllocationId
	COMMIT

	
