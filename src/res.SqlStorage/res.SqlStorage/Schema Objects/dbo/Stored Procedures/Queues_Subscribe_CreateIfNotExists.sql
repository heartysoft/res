CREATE PROCEDURE [dbo].[Queues_Subscribe_CreateIfNotExists]
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
			(SELECT Min(GlobalSequence) FROM [Events] WHERE
				ContextName = @Context 
					AND StreamId LIKE (@Filter + '%')
					AND TimeStamp >= @StartTime
			),
			-9223372036854775808));
