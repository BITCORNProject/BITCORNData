CREATE TABLE [dbo].[UserStat] (
    [UserId] INT NOT NULL,
    [Tipped] INT NULL DEFAULT null, 
    [TIppedTotal] NUMERIC(19, 8) NULL DEFAULT null, 
    [TopTiped] NUMERIC(19, 8) NULL DEFAULT null, 
    [Tip] INT NULL DEFAULT null, 
    [TipTotal] NUMERIC(19, 8) NULL DEFAULT null, 
    [TopTip] NUMERIC(19, 8) NULL DEFAULT null, 
    [Rained] INT NULL DEFAULT null, 
    [RainTotal] NUMERIC(19, 8) NULL DEFAULT null, 
    [TopRain] NUMERIC(19, 8) NULL DEFAULT null, 
    [RainedOn] INT NULL DEFAULT null, 
    [RainedOnTotal] NUMERIC(19, 8) NULL DEFAULT null, 
    [TopRainedOn] NUMERIC(19, 8) NULL DEFAULT null, 
    PRIMARY KEY CLUSTERED ([UserId] ASC), 
    CONSTRAINT [FK_UserStat_Users] FOREIGN KEY ([UserId]) REFERENCES [User]([UserId])
);

