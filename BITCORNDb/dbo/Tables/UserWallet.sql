CREATE TABLE [dbo].[UserWallet] (
    [UserId]       INT             NOT NULL,
    [WalletServer] INT             NULL DEFAULT null,
    [Balance]      NUMERIC (19, 8) NULL DEFAULT null,
    [CornAddy]     VARCHAR (50)    NULL DEFAULT null,
    PRIMARY KEY CLUSTERED ([UserId] ASC),
    CONSTRAINT [FK_UserWallet_User] FOREIGN KEY ([UserId]) REFERENCES [dbo].[User] ([UserId])
);

