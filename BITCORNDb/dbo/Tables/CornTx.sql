CREATE TABLE [dbo].[CornTx]
(
	[CornTxId] INT NOT NULL PRIMARY KEY, 
    [Platform] VARCHAR(50) NULL DEFAULT null, 
    [TxType] VARCHAR(50) NULL DEFAULT null, 
    [Amount] NUMERIC(19, 8) NULL DEFAULT null, 
    [SenderId] VARCHAR(100) NULL DEFAULT null, 
    [ReceiverId] VARCHAR(100) NULL DEFAULT null, 
    [Timestamp] DATETIME NULL DEFAULT null, 
    [BlockchainTxId] NVARCHAR(100) NULL DEFAULT null
)
