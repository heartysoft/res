use [{DatabaseName}];
GO

CREATE TABLE [dbo].[Events](
	[EventId] [uniqueidentifier] NOT NULL,
	[StreamId] [nvarchar](256) NOT NULL,
	[ContextName] [nvarchar](50) NOT NULL,
	[Sequence] [bigint] NOT NULL,
	[GlobalSequence] BIGINT NOT NULL IDENTITY(-9223372036854775808, 1),
	[TimeStamp] [datetime2](4) NOT NULL,
	[EventType] [nvarchar](500) NOT NULL,
    [Headers] [nvarchar](max) NULL,
	[Body] [nvarchar](max) NOT NULL, 
    CONSTRAINT [PK_Events] PRIMARY KEY ([GlobalSequence])
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO

CREATE NONCLUSTERED INDEX [IX_TimeStamp] ON [dbo].[Events]
(
	[TimeStamp] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Events] ON [dbo].[Events]
(
	[ContextName] ASC,
	[StreamId] ASC,
	[Sequence] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


SET Identity_insert [Events] ON;
insert into [Events] (EventId, StreamId, ContextName, [Sequence], GlobalSequence, [TimeStamp], EventType, Body) 
select EventId, StreamId, ContextName, [Sequence], GlobalSequence, [TimeStamp], EventType, Body from EventWrappers;

SET Identity_insert [Events] OFF;

GO

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


DROP table QueueAllocations;
GO

CREATE TABLE [dbo].[QueueAllocations]
(
	[AllocationId] BIGINT NOT NULL IDENTITY(-9223372036854775808, 1),
    [Context] nvarchar(50) NOT NULL,
	[QueueId] nvarchar(50) NOT NULL,
	[SubscriberId] nvarchar(50) NOT NULL,
	[ExpiresAt] datetime2(4) NOT NULL,
	[StartMarker] bigint NOT NULL,
	[EndMarker] bigint NOT NULL, 
    CONSTRAINT [PK_QueueAllocations] PRIMARY KEY ([AllocationId])
)

GO

CREATE INDEX [IX_QueueAllocations_Context_QueueId_SubscriberId] ON [dbo].[QueueAllocations] ([Context], [QueueId], [SubscriberId])

GO


DROP PROCEDURE [dbo].[AppendEvents]
DROP PROCEDURE [dbo].[FetchEvent]
DROP PROCEDURE [dbo].[LoadEvents]
DROP PROCEDURE [dbo].[Queues_Acknowledge]
DROP PROCEDURE [dbo].[Queues_Acknowledge_Acknowledge]
DROP PROCEDURE [dbo].[Queues_Subscribe]
DROP PROCEDURE [dbo].[Queues_Subscribe_Allocate]
DROP PROCEDURE [dbo].[Queues_Subscribe_CreateIfNotExists]
DROP PROCEDURE [dbo].[Queues_Subscribe_FetchEvents]
DROP PROCEDURE [dbo].[Queues_GetByDecreasingNextMarker]

DROP Type EventParameter;
GO


CREATE TYPE [dbo].[EventParameter] AS TABLE(
	[EventId] [uniqueidentifier] NOT NULL,
	[StreamId] [nvarchar](256) NOT NULL,
	[ContextName] [nvarchar](50) NOT NULL,
	[Sequence] [bigint] NULL,
	[SequenceInCommit] [int],
	[TimeStamp] [datetime2](4) NULL,
	[EventType] [nvarchar](500) NULL,
    [Headers] [nvarchar](max) NULL,
	[Body] [nvarchar](max) NULL,
	[CommitId] [uniqueidentifier] NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[EventId] ASC,
	[StreamId] ASC,
	[ContextName] ASC,
	[CommitId] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO




CREATE PROCEDURE [dbo].[AppendEvents]
	@Events EventParameter ReadOnly
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
	DECLARE @FailedCommits TABLE(RowId int);
	
	Declare @Commits TABLE(
		RowId int not null identity(1,1),
		CommitId uniqueidentifier, 
		MinSequence bigint, 
		MaxSequence bigint, 
		EventCount int,
		Context nvarchar(50), 
		Stream nvarchar(256),
		Timestamp datetime2(4),
		Unique Clustered (CommitId, Context, Stream)
		);

    DECLARE @CommitCount int;
	Declare @CurrentCommit int;

	Insert Into @Commits (CommitId, Context, Stream, MinSequence, MaxSequence, EventCount, Timestamp)
	SELECT CommitId, ContextName as Context, StreamId as Stream, Min(Sequence), Max(Sequence), Count(EventId), Min(Timestamp) as MinTimestamp
		FROM @Events
		Group By CommitId, ContextName, StreamId
		order by MinTimestamp;
	
	SET @CommitCount = @@ROWCOUNT;
	SET @CurrentCommit = 0;

	WHILE @CurrentCommit < @CommitCount
	BEGIN
		SET @CurrentCommit = @CurrentCommit + 1;
		
		Begin Try			
			Begin TRAN;
				With toInsert (Stream, Context, Sequence) as
				(SELECT m.Stream, m.Context as Context, (case m.MaxSequence when -1 then COALESCE(s.CurrentSequence, 0) + m.EventCount else m.MaxSequence end) as Sequence 
					from @Commits m left outer join Streams s
					on m.Stream = s.StreamId AND m.Context = s.Context
					WHERE
						(m.RowId = @CurrentCommit) AND 
						(m.MinSequence=-1 OR m.MinSequence = (s.CurrentSequence + 1) OR (s.CurrentSequence is null AND m.MinSequence = 1)))
				Merge Streams as target
				using toInsert as source
				on (target.StreamId = source.Stream AND target.Context = source.Context)
				When Matched Then
					Update SET CurrentSequence = source.Sequence
				When NOT MATCHED BY target then 
					Insert (StreamId, CurrentSequence, Context) VALUES (source.Stream, source.Sequence, source.Context)
				;

				IF @@ROWCOUNT > 0 
				Begin
					Insert Into [Events] (EventId, ContextName, StreamId, Sequence, Timestamp, EventType, Headers, Body)
					SELECT e.EventId, e.ContextName, e.StreamId, case e.Sequence when -1 then (s.CurrentSequence - c.EventCount + e.SequenceInCommit) else e.Sequence end, e.TimeStamp, e.EventType, e.Headers, e.Body 
					from @Events e inner join @Commits c
						on e.CommitId = c.CommitId 
						AND
						c.RowId = @CurrentCommit inner join Streams s on
						e.ContextName = s.Context AND e.StreamId = s.StreamID
					order by e.SequenceInCommit;
				End
				else
				Begin
					 Insert into @FailedCommits VALUES (@CurrentCommit)
				End
			Commit TRAN;
		End Try
		Begin Catch
			Rollback TRAN;
			Insert into @FailedCommits VALUES (@CurrentCommit)
		End Catch;

	END

    SELECT c.CommitId from @Commits c inner join @FailedCommits sc
		on c.RowId = sc.RowId
END


GO


CREATE PROCEDURE [dbo].[FetchEvent]
	-- Add the parameters for the stored procedure here
	@Events EventRequestParameter READONLY
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	SELECT r.RequestId, e.* from [Events] e inner join @Events r on
		e.EventId = r.EventId
		AND e.ContextName = r.ContextName
		AND e.StreamId = r.StreamId

END

GO

CREATE PROCEDURE [dbo].[LoadEvents]
	-- Add the parameters for the stored procedure here
	@Context nvarchar(50),
	@Stream nvarchar(256),
	@FromVersion bigint = 1,
	@ToVersion bigint = NULL
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	IF @ToVersion Is Not NULL
	Begin
		SELECT * from [Events]
			WHERE ContextName = @Context AND
			StreamId = @Stream AND
			Sequence BETWEEN @FromVersion AND @ToVersion
		ORDER BY Sequence
	End
	Else
	Begin
		SELECT * from [Events]
			WHERE ContextName = @Context AND
			StreamId = @Stream AND
			Sequence >= @FromVersion
		ORDER BY Sequence
	End 
END


GO

CREATE PROCEDURE [dbo].[Queues_Acknowledge_Acknowledge]
	@AllocationId bigint
AS
	DELETE FROM QueueAllocations 
		WHERE AllocationId = @AllocationId

GO

CREATE PROCEDURE [dbo].[Queues_GetByDecreasingNextMarker]
	@Count int,
	@Skip int
AS
	SELECT TOP(@Count) Context, QueueId, Filter, NextMarker FROM Queues
		Order By NextMarker DESC 

GO

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

GO


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

GO

CREATE PROCEDURE [dbo].[Queues_Subscribe_FetchEvents]
	@AllocationId bigint
AS
	SELECT ew.* from QueueAllocations qa 
		inner join Queues qs
			on 
            qa.Context = qs.Context
                AND qa.QueueId = qs.QueueId 
		inner join [Events] ew on	
			ew.ContextName = qs.Context AND
			(ew.GlobalSequence BETWEEN qa.StartMarker AND qa.EndMarker) AND
			(qs.Filter = '*' OR ew.StreamId LIKE (qs.Filter + '%'))
	WHERE
		qa.AllocationId = @AllocationId
	Order By ew.TimeStamp

GO


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


GO

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

GO






