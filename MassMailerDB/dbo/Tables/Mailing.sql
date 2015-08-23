CREATE TABLE [dbo].[Mailing] (
    [ID]                INT            IDENTITY (1, 1) NOT NULL,
    [MailStateID]       INT            NULL,
    [TemplateID]        BIGINT         NOT NULL,
    [StateChangeMoment] DATETIME       NOT NULL,
    [Name]              VARCHAR(250)	NOT NULL,
    [Subject]           VARCHAR(250)	NOT NULL,
    [AddressFrom]       VARCHAR(250)	NULL,
    [Priority]          INT            NOT NULL,
    CONSTRAINT [PK_Mailing] PRIMARY KEY CLUSTERED ([ID] ASC),
    FOREIGN KEY ([MailStateID]) REFERENCES [dbo].[MailState] ([ID])
);

