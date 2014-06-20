CREATE PROCEDURE [dbo].[Queues_GetByDecreasingNextMarker]
	@Count int,
	@Skip int
AS
	SELECT TOP(@Count) * FROM Queues
		Order By NextMarker DESC 
