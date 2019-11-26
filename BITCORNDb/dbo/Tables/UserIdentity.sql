CREATE TABLE [dbo].[UserIdentity] (
    [UserId]         INT           NOT NULL,
    [TwitchUsername] VARCHAR (100) NULL DEFAULT null,
	[Auth0Nickname]  VARCHAR (100) NULL DEFAULT null,
    [Auth0Id]        VARCHAR (100) NULL DEFAULT null,
    [Twitchid]       VARCHAR (100) NULL DEFAULT null,
    [Discordid]      VARCHAR (100) NULL DEFAULT null,
    [Twitterid]      VARCHAR (100) NULL DEFAULT null,
    [Redditid]       VARCHAR (100) NULL DEFAULT null,

    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserIdentity_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([UserId])
);

