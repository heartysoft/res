-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
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