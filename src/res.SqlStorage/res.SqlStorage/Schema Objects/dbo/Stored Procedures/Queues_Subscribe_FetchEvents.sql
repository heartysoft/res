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
