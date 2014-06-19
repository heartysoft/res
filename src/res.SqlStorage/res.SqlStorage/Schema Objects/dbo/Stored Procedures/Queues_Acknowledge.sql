CREATE PROCEDURE [dbo].[Queues_Acknowledge]
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@AllocationId bigint = NULL,
	@Count int,
	@AllocationTimeInMilliseconds int
AS
	Declare @Now datetime2(4) = GetUtcDate()
	Declare @NewAllocationId bigint = NULL
	
	IF @AllocationId IS NOT NULL
		EXEC Queues_Acknowledge_Acknowledge @AllocationId

	BEGIN TRAN	
		Exec Queues_Subscribe_Allocate @QueueId, @SubscriberId, @Count, @AllocationTimeInMilliseconds, @Now, @AllocationId
	COMMIT
		
	Exec Queues_Subscribe_FetchEvents @NewAllocationId
	SELECT @NewAllocationId AS AllocationId
	


