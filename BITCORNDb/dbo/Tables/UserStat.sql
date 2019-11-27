CREATE TABLE [dbo].[UserStat] (
    [UserId] INT NOT NULL,
    [Tipped] INT NULL DEFAULT 0, 
    [TIppedTotal] NUMERIC(19, 8) NULL DEFAULT 0, 
    [TopTiped] NUMERIC(19, 8) NULL DEFAULT 0, 
    [Tip] INT NULL DEFAULT 0, 
    [TipTotal] NUMERIC(19, 8) NULL DEFAULT 0, 
    [TopTip] NUMERIC(19, 8) NULL DEFAULT 0, 
    [Rained] INT NULL DEFAULT 0, 
    [RainTotal] NUMERIC(19, 8) NULL DEFAULT 0, 
    [TopRain] NUMERIC(19, 8) NULL DEFAULT 0, 
    [RainedOn] INT NULL DEFAULT 0, 
    [RainedOnTotal] NUMERIC(19, 8) NULL DEFAULT 0, 
    [TopRainedOn] NUMERIC(19, 8) NULL DEFAULT 0, 
    [EarnedIdle] NUMERIC(19, 8) NULL DEFAULT 0, 
    PRIMARY KEY CLUSTERED ([UserId] ASC), 
    CONSTRAINT [FK_UserStat_Users] FOREIGN KEY ([UserId]) REFERENCES [User]([UserId])
);

