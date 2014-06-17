CREATE PROCEDURE [dbo].[Queues_Acknowledge_Acknowledge]
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@StartMarker bigint,
	@EndMarker bigint
AS
	DELETE FROM QueueAllocations 
		WHERE QueueId = @QueueId
			AND SubscriberId = @SubscriberId
			AND StartMarker = @StartMarker
			AND	EndMarker = @EndMarker
RETURN 0
