CREATE PROCEDURE [dbo].[Queues_Acknowledge_Acknowledge]
	@AllocationId bigint
AS
	DELETE FROM QueueAllocations 
		WHERE AllocationId = @AllocationId
