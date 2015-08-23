CREATE TABLE [dbo].[Sets] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Name]        VARCHAR(250) NOT NULL,
    [Description] VARCHAR(250) NULL,
    [Date]        DATETIME       NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 100)
);


GO
CREATE STATISTICS [_WA_Sys_00000004_5772F790]
    ON [dbo].[Sets]([Date]);

