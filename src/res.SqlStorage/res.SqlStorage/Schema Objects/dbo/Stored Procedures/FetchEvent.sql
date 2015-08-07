-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
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
