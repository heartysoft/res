CREATE TABLE [dbo].[Subscriptions](
	[SubscriptionId] [bigint] IDENTITY(1,1) NOT NULL,
	[Context] [nvarchar](50) NOT NULL,
	[Subscriber] [nvarchar](50) NOT NULL,
	[CurrentBookmark] [datetime2](4) NOT NULL,
	[NextBookmark] [datetime2](4) NOT NULL,
	[LastActive] [datetime2](4) NOT NULL,
	[Filter] [nvarchar](200) NOT NULL,
 CONSTRAINT [PK_Subscriptions] PRIMARY KEY CLUSTERED 
(
	[SubscriptionId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]