-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- =============================================
-- Author:		Ashic Mahtab
-- Create date: 9 May, 2014
-- Description:	Sets the subscription to a specified time.
-- =============================================
CREATE PROCEDURE SetSubscription
	-- Add the parameters for the stored procedure here
	@SubscriptionId bigint,
	@ResetTo datetime2(4),
	@ExpectedNextBookmark datetime2(4),
	@CurrentTime datetime2(4)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	Update Subscriptions Set CurrentBookmark = @ResetTo, NextBookmark=@ResetTo, LastActive = @CurrentTime
	WHERE
	SubscriptionId = @SubscriptionId AND NextBookmark = @ExpectedNextBookmark
END
GO
