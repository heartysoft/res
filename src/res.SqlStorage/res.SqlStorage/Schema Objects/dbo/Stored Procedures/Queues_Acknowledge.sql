CREATE PROCEDURE [dbo].[Queues_Acknowledge]
    @Context nvarchar(50),
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

	IF @AllocationTimeInMilliseconds <> -1 BEGIN
		BEGIN TRAN	
			Exec Queues_Subscribe_Allocate @Context, @QueueId, @SubscriberId, @Count, @AllocationTimeInMilliseconds, @Now, @AllocationId = @NewAllocationId OUTPUT
		COMMIT
	END 
		
	Exec Queues_Subscribe_FetchEvents @NewAllocationId
	SELECT @NewAllocationId AS AllocationId
	


