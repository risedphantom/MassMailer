CREATE TABLE [dbo].[Template] (
    [ID]           BIGINT           IDENTITY (1, 1) NOT NULL,
    [Name]         VARCHAR (8000)   NOT NULL,
    [Description]  VARCHAR (8000)   NULL,
    [Body]         TEXT             NOT NULL,
    [ChangeMoment] DATETIME         NOT NULL,
    [IsHTML]       BIT              DEFAULT ((1)) NOT NULL,
    [GUID]         UNIQUEIDENTIFIER DEFAULT (newid()) NOT NULL,
    CONSTRAINT [PK_Template] PRIMARY KEY CLUSTERED ([ID] ASC)
);

