CREATE TABLE [dbo].[UserIdentity] (
    [UserId]         INT           NOT NULL,
    [TwitchUsername] VARCHAR (100) NULL DEFAULT null,
	[Auth0Nickname]  VARCHAR (100) NULL DEFAULT null,
    [Auth0Id]        VARCHAR (100) NULL DEFAULT null,
    [TwitchId]       VARCHAR (100) NULL DEFAULT null,
    [DiscordId]      VARCHAR (100) NULL DEFAULT null,
    [TwitterId]      VARCHAR (100) NULL DEFAULT null,
    [RedditId]       VARCHAR (100) NULL DEFAULT null,

    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserIdentity_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([UserId])
);

