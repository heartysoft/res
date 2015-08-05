use [{DatabaseName}];


DECLARE @SQL VARCHAR(4000)
SET @SQL = 'ALTER TABLE Queues DROP CONSTRAINT |ConstraintName| '

SET @SQL = REPLACE(@SQL, '|ConstraintName|', ( SELECT   name
                                               FROM     sysobjects
                                               WHERE    xtype = 'PK'
                                                        AND parent_obj = OBJECT_ID('Queues')
                                             ))

EXEC (@SQL)


ALTER TABLE Queues
ADD CONSTRAINT PK_Queues_Id_Context PRIMARY KEY (Context, QueueId)

GO

ALTER TABLE QueueAllocations
ADD Context nvarchar(50);
GO

DROP INDEX [IX_QueueAllocations_QueueId_SubscriberId] on [dbo].[QueueAllocations]
GO

CREATE INDEX [IX_QueueAllocations_Context_QueueId_SubscriberId] ON [dbo].[QueueAllocations] ([Context], [QueueId], [SubscriberId])
GO




ALTER PROCEDURE [dbo].[Queues_Acknowledge]
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
	
GO

ALTER PROCEDURE [dbo].[Queues_Acknowledge_Acknowledge]
	@AllocationId bigint
AS
	DELETE FROM QueueAllocations 
		WHERE AllocationId = @AllocationId

GO

ALTER PROCEDURE [dbo].[Queues_GetByDecreasingNextMarker]
	@Count int,
	@Skip int
AS
	SELECT TOP(@Count) Context, QueueId, Filter, NextMarker FROM Queues
		Order By NextMarker DESC 

GO

ALTER PROCEDURE [dbo].[Queues_Subscribe]
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

GO

ALTER PROCEDURE [dbo].[Queues_Subscribe_Allocate]
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
				(SELECT TOP (@Count) GlobalSequence FROM EventWrappers ew
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


GO

ALTER PROCEDURE [dbo].[Queues_Subscribe_CreateIfNotExists]
	@Context nvarchar(50),
	@QueueId nvarchar(50),
	@Filter nvarchar(256),
	@StartTime datetime2(4)
AS
	-- create a new queue with queueid if it doesn't exist.
	-- for creation, the NextMarker should be the lowest
	-- GlobalSequence number for events at time >= @StartTime

	IF NOT EXISTS (SELECT * FROM Queues
		WHERE Context = @Context
			AND QueueId = @QueueId
			AND Filter = @Filter)
	INSERT INTO Queues (Context, QueueId, Filter, NextMarker)
	VALUES (@Context, @QueueId, @Filter, 
		COALESCE(
			(SELECT Min(GlobalSequence) FROM EventWrappers WHERE
				ContextName = @Context 
					AND StreamId LIKE (@Filter + '%')
					AND TimeStamp >= @StartTime
			),
			-9223372036854775808));



GO

ALTER PROCEDURE [dbo].[Queues_Subscribe_FetchEvents]
	@AllocationId bigint
AS
	SELECT ew.* from QueueAllocations qa 
		inner join Queues qs
			on 
            qa.Context = qs.Context
                AND qa.QueueId = qs.QueueId 
		inner join EventWrappers ew on	
			ew.ContextName = qs.Context AND
			(ew.GlobalSequence BETWEEN qa.StartMarker AND qa.EndMarker) AND
			(qs.Filter = '*' OR ew.StreamId LIKE (qs.Filter + '%'))
	WHERE
		qa.AllocationId = @AllocationId
	Order By ew.TimeStamp


GO
