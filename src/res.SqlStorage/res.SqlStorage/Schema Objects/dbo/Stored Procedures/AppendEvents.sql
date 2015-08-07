-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[AppendEvents]
	-- Add the parameters for the stored procedure here
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
