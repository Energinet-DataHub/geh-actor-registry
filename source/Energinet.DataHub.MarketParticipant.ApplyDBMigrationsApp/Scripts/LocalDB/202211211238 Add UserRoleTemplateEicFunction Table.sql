SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[UserRoleTemplateEicFunction](
    [UserRoleTemplateId] [uniqueidentifier] NOT NULL,
    [EicFunction] [int] NOT NULL,
     CONSTRAINT [PK_UserRoleTemplateEicFunction] PRIMARY KEY CLUSTERED
    (
    [UserRoleTemplateId] ASC,
[EicFunction] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]
    GO