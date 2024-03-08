CREATE TABLE [dbo].[ActorDelegation]
(
    [Id] [uniqueidentifier] NOT NULL,
    [DelegatedByActorId] [uniqueidentifier] NOT NULL,
    [DelegatedToActorId] [uniqueidentifier] NOT NULL,
    [GridAreaId] [uniqueidentifier] NOT NULL,
    [MessageType] [int] NOT NULL,
    [StartsAt] [datetimeoffset](7) NOT NULL,
    [ExpiresAt] [datetimeoffset](7) NULL,
    CONSTRAINT [PK_ActorDelegation] PRIMARY KEY CLUSTERED
    (
        [Id] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ActorDelegation] WITH CHECK ADD CONSTRAINT [FK_ActorDelegationGridAreaId_GridArea] FOREIGN KEY([GridAreaId])
    REFERENCES [dbo].[GridArea] ([Id])
