CREATE PROCEDURE [dbo].[Queues_Subscribe_Allocate]
    @Context nvarchar(50),
	@QueueId nvarchar(50),
	@SubscriberId nvarchar(50),
	@Count int,
	@AllocationTimeInMilliseconds int,
	@Now datetime2(4),
	@AllocationId bigint output
AS
	Declare @ExpiresAt datetime2(4)

	SELECT top (1) @AllocationId = AllocationId FROM QueueAllocations
		WHERE
                Context = @Context 
                AND QueueId = @QueueId
				AND SubscriberId = @SubscriberId
				AND ExpiresAt > @Now
	
	IF @AllocationId IS NOT NULL
		RETURN 0
	ELSE BEGIN
		SELECT TOP(1) @AllocationId = AllocationId FROM QueueAllocations
				WHERE 
                    Context=  @Context
                    AND QueueId = @QueueId 
					AND ExpiresAt <= @Now	
				Order By StartMarker

		IF @AllocationId IS NOT NULL  -- Expired allocation for this queue exists. Re-allocate.
			BEGIN
				SET @ExpiresAt = DateAdd(ms, @AllocationTimeInMilliseconds, @Now)
				UPDATE top(1) QueueAllocations 
					SET SubscriberId = @SubscriberId, 
						ExpiresAt = @ExpiresAt
				WHERE AllocationId = @AllocationId

				RETURN 0;
			END 
		ELSE BEGIN -- No unexpired allocation for current subscriber, and no available expired allocation. Need new allocation.
			DECLARE @StartMark bigint, @EndMark bigint
		
			SELECT @StartMark = Min(T.GlobalSequence), @EndMark = Max(T.GlobalSequence) FROM 
				(SELECT TOP (@Count) GlobalSequence FROM [Events] ew
					INNER JOIN Queues qs
						on ew.ContextName = qs.Context
						AND (qs.Filter = '*' OR ew.StreamId LIKE (qs.Filter + '%'))
					WHERE
						ew.GlobalSequence >= qs.NextMarker
						AND
                        qs.Context = @Context
                        AND
						qs.QueueId = @QueueId
					Order By GlobalSequence
						) AS T
		
			IF @StartMark IS NULL -- No events for new allocation. Do Nothing.
				RETURN 0
			ELSE BEGIN -- Events present for new allocation. Allocate, and update Next marker for queue.
				SET @ExpiresAt = DateAdd(ms, @AllocationTimeInMilliseconds, @Now)

				UPDATE Queues SET NextMarker = (@EndMark + 1)
					WHERE
                        Context = @Context 
                        AND 
                        QueueId = @QueueId

				INSERT INTO QueueAllocations (Context, QueueId, SubscriberId, ExpiresAt, StartMarker, EndMarker)
					VALUES (@Context, @QueueId, @SubscriberId, @ExpiresAt, @StartMark, @EndMark)

				SET @AllocationId = @@IDENTITY
				RETURN 0
			END
		END
	END
RETURN 0