ALTER TABLE [dbo].[User]
    ADD [AdministratedByActorId] [uniqueidentifier] NOT NULL DEFAULT('00000000-0000-0000-0000-000000000000')
GO
