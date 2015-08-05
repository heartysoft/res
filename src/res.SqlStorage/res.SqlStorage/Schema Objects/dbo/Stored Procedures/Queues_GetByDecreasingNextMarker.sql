CREATE PROCEDURE [dbo].[Queues_GetByDecreasingNextMarker]
	@Count int,
	@Skip int
AS
	SELECT TOP(@Count) Context, QueueId, Filter, NextMarker FROM Queues
		Order By NextMarker DESC 
