CREATE TABLE [dbo].[Attachment] (
    [ID]    BIGINT                     IDENTITY (1, 1) NOT NULL,
    [RowID] UNIQUEIDENTIFIER           DEFAULT (newsequentialid()) ROWGUIDCOL NOT NULL,
    [Name]  VARCHAR (8000)             NULL,
    [Data]  VARBINARY (MAX) FILESTREAM NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC),
    UNIQUE NONCLUSTERED ([RowID] ASC)
) FILESTREAM_ON [FileStreamGroup];

