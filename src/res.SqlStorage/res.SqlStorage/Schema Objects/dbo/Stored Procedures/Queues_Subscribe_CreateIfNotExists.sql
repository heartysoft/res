CREATE PROCEDURE [dbo].[Queues_Subscribe_CreateIfNotExists]
	@QueueId nvarchar(50),
	@Context nvarchar(50),
	@Filter nvarchar(256),
	@StartTime datetime2(4)
AS
	-- create a new queue with queueid if it doesn't exist.
	-- for creation, the NextMarker should be the lowest
	-- GlobalSequence number for events at time >= @StartTime

	IF NOT EXISTS (SELECT * FROM Queues
		WHERE QueueId = @QueueId 
			AND Context = @Context
			AND Filter = @Filter)
	INSERT INTO Queues (QueueId, Context, Filter, NextMarker)
	VALUES (@QueueId, @Context, @Filter, 
		COALESCE(
			(SELECT Min(GlobalSequence) FROM EventWrappers WHERE
				ContextName = @Context 
					AND StreamId LIKE (@Filter + '%')
					AND TimeStamp >= @StartTime
			),
			-9223372036854775808));
