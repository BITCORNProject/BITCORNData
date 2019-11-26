CREATE TABLE [dbo].[UnclaimedTx]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Expiration] DATETIME NULL DEFAULT null, 
    [SenderUserId] INT NULL DEFAULT null, 
    [CornTxId] INT NULL DEFAULT null, 
    [Claimed] BIT NULL DEFAULT null, 
    [ReceiverUserId] INT NULL DEFAULT null, 
    [Platform] NCHAR(10) NULL DEFAULT null, 
    [Amount] NUMERIC(19, 8) NULL DEFAULT null, 
    [Refunded] BIT NULL DEFAULT 0, 
    [TxType] VARCHAR(50) NULL DEFAULT null, 
    [Timestamp] DATETIME NULL DEFAULT null, 
    CONSTRAINT [FK_UnclaimedTx_CornTx] FOREIGN KEY ([CornTxId]) REFERENCES [dbo].[CornTx] ([CornTxId]), 
    CONSTRAINT [FK_UnclaimedTx_SendUser] FOREIGN KEY ([SenderUserId]) REFERENCES [User]([UserId]), 
    CONSTRAINT [FK_UnclaimedTx_User] FOREIGN KEY ([ReceiverUserId]) REFERENCES [User]([UserId]) 
)
