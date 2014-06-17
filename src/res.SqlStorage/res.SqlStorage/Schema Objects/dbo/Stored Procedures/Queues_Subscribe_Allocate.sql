CREATE PROCEDURE [dbo].[Queues_Subscribe_Allocate]
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@Count int,
	@Timeout datetime2(4),
	@Now datetime2(4)
AS

	IF EXISTS -- unexpired allocation for this subscriber if exists. do nothing.
		(SELECT * FROM QueueAllocations
			WHERE QueueId = @QueueId
				AND SubscriberId = SubscriberId
				AND ExpiresAt > @Now)
	Return 0


	-- select lowest StartMark from QueueAllocations
	-- Where QueueId = @QueueId and ExpiresAt <= Now
	
	Declare @LowestStartMark int 
	SELECT @LowestStartMark = Min(StartMarker) FROM QueueAllocations
			WHERE QueueId = @QueueId 
				AND ExpiresAt <= @Now	

	IF @LowestStartMark IS NOT NULL  -- Expired allocation for this queue exists. Re-allocate.
		UPDATE QueueAllocations 
			SET SubscriberId = @SubscriberId, 
				ExpiresAt = @Timeout
		WHERE StartMarker = @LowestStartMark
	ELSE BEGIN -- No unexpired allocation for current subscriber, and no available expired allocation. Need new allocation.
		DECLARE @StartMark int, @EndMark int
		
		SELECT @StartMark = Min(T.GlobalSequence), @EndMark = Max(T.GlobalSequence) FROM 
			(SELECT TOP (@Count) GlobalSequence FROM EventWrappers ew
				INNER JOIN Queues qs
					on ew.ContextName = qs.Context
					AND (qs.Filter = '*' OR ew.StreamId LIKE (qs.Filter + '%'))
				WHERE
					ew.GlobalSequence >= qs.NextMarker) AS T
		
		IF @StartMark IS NULL -- No events for new allocation. Do Nothing.
			RETURN 0
		ELSE BEGIN -- Events present for new allocation. Allocate, and update Next marker for queue.
			UPDATE Queues SET NextMarker = (@EndMark + 1)
				WHERE @QueueId = @QueueId

			INSERT INTO QueueAllocations (QueueId, SubscriberId, ExpiresAt, StartMarker, EndMarker)
				VALUES (@QueueId, @SubscriberId, @Timeout, @StartMark, @EndMark)
		END
	END
		
			
RETURN 0
