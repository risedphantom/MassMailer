CREATE TABLE [dbo].[ClientSet] (
    [ClientID] INT NOT NULL,
    [SetID]    INT NOT NULL,
    FOREIGN KEY ([SetID]) REFERENCES [dbo].[Sets] ([ID]),
    FOREIGN KEY ([SetID]) REFERENCES [dbo].[Sets] ([ID])
);


GO
CREATE STATISTICS [_WA_Sys_00000001_3DB3258D]
    ON [dbo].[ClientSet]([ClientID]);


GO
CREATE STATISTICS [_WA_Sys_00000002_3DB3258D]
    ON [dbo].[ClientSet]([SetID]);

