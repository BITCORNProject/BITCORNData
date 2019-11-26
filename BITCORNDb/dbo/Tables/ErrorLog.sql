CREATE TABLE [dbo].[ErrorLogs]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY, 
    [Application] VARCHAR(100) NULL DEFAULT null, 
    [Message] VARCHAR(1000) NULL DEFAULT null, 
    [StackTrace] VARCHAR(5000) NULL DEFAULT null, 
    [Code] VARCHAR(100) NULL DEFAULT null, 
    [TImestamp] DATETIME NULL DEFAULT null
)
