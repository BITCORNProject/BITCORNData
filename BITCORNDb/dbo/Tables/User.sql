CREATE TABLE [dbo].[User] (
    [UserId]   INT            IDENTITY (420000000, 1) NOT NULL,
    [Level]    VARCHAR (50)   NULL DEFAULT null,
    [Username] VARCHAR(50)  NULL DEFAULT null,
    [Avatar]   VARCHAR (2048) NULL DEFAULT null,
    PRIMARY KEY CLUSTERED ([UserId] ASC)
);

