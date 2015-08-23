CREATE TABLE [dbo].[Archive] (
    [TemplateID]    BIGINT         NOT NULL,
    [XMLData]       XML            NULL,
    [AddressFrom]   VARCHAR (8000) NULL,
    [AddressTo]     VARCHAR (8000) NULL,
    [Subject]       VARCHAR (8000) NULL,
    [Priority]      INT            NOT NULL,
    [Status]        INT            NOT NULL,
    [MissionID]     INT            NULL,
    [ExternalOwnerID]  BIGINT         NOT NULL,
    [SendMoment]    DATETIME       NULL,
    [AddMoment]     DATETIME       NULL,
    [Host]          VARCHAR (128)  NULL,
    [MailingID]     INT            NULL,
    [AddressCC]     VARCHAR (8000) NULL,
    [HasAttachment] BIT            DEFAULT ((0)) NOT NULL,
    [ID]            BIGINT         NULL
);


GO
CREATE NONCLUSTERED INDEX [IX_ID]
    ON [dbo].[Archive]([ID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_MissionID_Status]
    ON [dbo].[Archive]([MissionID] ASC, [Status] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_MailingID_MissionID]
    ON [dbo].[Archive]([MailingID] ASC, [MissionID] ASC);

