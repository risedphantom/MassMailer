CREATE TABLE [dbo].[Mission] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL,
    [MailingID]         INT            NULL,
    [State]             INT            NOT NULL,
    [StateChangeMoment] DATETIME       NOT NULL,
    [Test]              BIT            NOT NULL,
    [SetID]             INT            NULL,
    [MailGroupID]       INT            NULL,
    [User]              VARCHAR (8000) NOT NULL,
    [ListID]            VARCHAR (255)  NULL,
    CONSTRAINT [PK_Mission] PRIMARY KEY CLUSTERED ([ID] ASC),
    FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    FOREIGN KEY ([MailGroupID]) REFERENCES [dbo].[MailGroup] ([ID]),
    FOREIGN KEY ([MailingID]) REFERENCES [dbo].[Mailing] ([ID]),
    FOREIGN KEY ([MailingID]) REFERENCES [dbo].[Mailing] ([ID]),
    FOREIGN KEY ([SetID]) REFERENCES [dbo].[Sets] ([ID]),
    FOREIGN KEY ([SetID]) REFERENCES [dbo].[Sets] ([ID]),
    FOREIGN KEY ([State]) REFERENCES [dbo].[MissionState] ([ID]),
    FOREIGN KEY ([State]) REFERENCES [dbo].[MissionState] ([ID])
);

