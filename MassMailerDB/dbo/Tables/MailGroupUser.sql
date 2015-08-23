CREATE TABLE [dbo].[MailGroupUser] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [MailGroupID] INT            NULL,
    [Email]       VARCHAR(250) NOT NULL,
    [FIO]         VARCHAR(250) NULL,
    [ClientID]    BIGINT         NULL,
    CONSTRAINT [PK__MailGrou__3214EC27694B04D5] PRIMARY KEY CLUSTERED ([ID] ASC) WITH (FILLFACTOR = 100),
    CONSTRAINT [FK__MailGroup__MailG__00200768] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    CONSTRAINT [FK__MailGroup__MailG__01142BA1] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    CONSTRAINT [FK__MailGroup__MailG__02084FDA] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    CONSTRAINT [FK__MailGroup__MailG__02FC7413] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    CONSTRAINT [FK__MailGroup__MailG__03F0984C] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    CONSTRAINT [FK__MailGroup__MailG__04E4BC85] FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID])
);


GO
CREATE NONCLUSTERED INDEX [XIF1MailGroupUser]
    ON [dbo].[MailGroupUser]([MailGroupID] ASC) WITH (FILLFACTOR = 100);

