CREATE TABLE [dbo].[MailState] (
    [ID]      INT            NOT NULL,
    [Name]    VARCHAR(250) NOT NULL,
    [SysName] VARCHAR(250) NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 100)
);


GO
CREATE STATISTICS [_WA_Sys_00000002_24927208]
    ON [dbo].[MailState]([Name]);

