CREATE TABLE [dbo].[MissionLog] (
    [MissionID]         INT            NULL,
    [State]             INT            NOT NULL,
    [StateChangeMoment] DATETIME       NOT NULL,
    [User]              VARCHAR (8000) NOT NULL,
    CONSTRAINT [MissionLogMissionID] FOREIGN KEY ([MissionID]) REFERENCES [dbo].[Mission] ([ID]),
    CONSTRAINT [MissionLogState] FOREIGN KEY ([State]) REFERENCES [dbo].[MissionState] ([ID])
);

