-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[ProgressSubscription]
	-- Add the parameters for the stored procedure here
	@SubscriptionId bigint,
	@ExpectedNextBookmark datetime2(4),
	@CurrentTime datetime2(4)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	Update Subscriptions Set CurrentBookmark = NextBookmark, LastActive = @CurrentTime
	WHERE
	SubscriptionId = @SubscriptionId AND NextBookmark = @ExpectedNextBookmark
END


GO