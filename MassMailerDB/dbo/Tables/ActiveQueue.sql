CREATE TABLE [dbo].[ActiveQueue] (
    [ID]            BIGINT         IDENTITY (1, 1) NOT NULL,
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
    [AddressCC]     VARCHAR (8000) NULL,
    [HasAttachment] BIT            DEFAULT ((0)) NOT NULL,
    CONSTRAINT [PK_ActiveQueue] PRIMARY KEY CLUSTERED ([ID] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IX_SendMoment_ID]
    ON [dbo].[ActiveQueue]([SendMoment] ASC, [ID] ASC);


GO
CREATE NONCLUSTERED INDEX [IX_ActiveQueue]
    ON [dbo].[ActiveQueue]([Status] ASC, [Priority] ASC);

