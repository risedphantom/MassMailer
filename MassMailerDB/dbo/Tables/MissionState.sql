CREATE TABLE [dbo].[MissionState] (
    [ID]      INT            IDENTITY (1, 1) NOT NULL,
    [SysName] VARCHAR(250) NOT NULL,
    [Name]    VARCHAR(250) NOT NULL,
    PRIMARY KEY CLUSTERED ([ID] ASC)
);

