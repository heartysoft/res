-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[CreateSubscriptionIdempotent]
	-- Add the parameters for the stored procedure here
	@Context nvarchar(50),
	@Subscriber nvarchar(50),
	@StartTime datetime2(4),
	@CurrentTime datetime2(4),
	@Filter nvarchar(200)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	Declare @ExistingSubscription bigint;
	SELECT @ExistingSubscription = SubscriptionId FROM Subscriptions
		where Subscriber = @Subscriber;

	if @ExistingSubscription is null 
	begin
		Insert Into Subscriptions (Context, Subscriber, CurrentBookmark, NextBookmark, LastActive, Filter)
		VALUES (@Context, @Subscriber, @StartTime, @StartTime, @CurrentTime, @Filter)
		SELECT CAST(@@IDENTITY AS bigint)
	end
	else
	 SELECT @ExistingSubscription

END





GO
