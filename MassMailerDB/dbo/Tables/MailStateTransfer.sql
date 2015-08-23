CREATE TABLE [dbo].[MailStateTransfer] (
    [ID]              INT IDENTITY (1, 1) NOT NULL,
    [FromMailStateID] INT NULL,
    [ToMailStateID]   INT NULL,
    [Active]          INT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 100),
    FOREIGN KEY ([FromMailStateID]) REFERENCES [dbo].[MailState] ([ID]),
    FOREIGN KEY ([ToMailStateID]) REFERENCES [dbo].[MailState] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [XIF2MailStateTransfer]
    ON [dbo].[MailStateTransfer]([ToMailStateID] ASC) WITH (FILLFACTOR = 100);


GO
CREATE NONCLUSTERED INDEX [XIF1MailStateTransfer]
    ON [dbo].[MailStateTransfer]([FromMailStateID] ASC) WITH (FILLFACTOR = 100);


GO
CREATE STATISTICS [_WA_Sys_00000004_300424B4]
    ON [dbo].[MailStateTransfer]([Active]);

