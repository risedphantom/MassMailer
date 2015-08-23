CREATE TABLE [dbo].[Priority] (
    [ID]          INT            NOT NULL,
    [Name]        VARCHAR (8000) NOT NULL,
    [Description] VARCHAR (8000) NOT NULL,
    CONSTRAINT [PK_Priority] PRIMARY KEY CLUSTERED ([ID] ASC)
);

