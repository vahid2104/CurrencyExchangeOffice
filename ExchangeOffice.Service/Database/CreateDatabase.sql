-- CreateDatabase.sql
-- Run this script in SQL Server (LocalDB) to create the CurrencyExchangeOfficeDb and required tables

IF DB_ID('CurrencyExchangeOfficeDb') IS NULL
BEGIN
	CREATE DATABASE CurrencyExchangeOfficeDb;
END

GO

USE CurrencyExchangeOfficeDb;
GO

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Users (
		UserId INT IDENTITY PRIMARY KEY,
		FullName NVARCHAR(100) NOT NULL,
		Username NVARCHAR(50) NULL,
		PasswordHash NVARCHAR(256) NULL,
		CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
	 );

	-- unique index on Username for non-null values
	CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username) WHERE Username IS NOT NULL;
END
GO

IF OBJECT_ID('dbo.Balances', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Balances (
		BalanceId INT IDENTITY PRIMARY KEY,
		UserId INT NOT NULL,
		CurrencyCode NVARCHAR(3) NOT NULL,
		Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
		CONSTRAINT UQ_UserCurrency UNIQUE (UserId, CurrencyCode),
		CONSTRAINT FK_Balances_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
	);
END
GO

-- If the Users table already exists, ensure username/password columns exist (safe migration)
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL
BEGIN
	IF COL_LENGTH('dbo.Users', 'Username') IS NULL
	BEGIN
		ALTER TABLE dbo.Users ADD Username NVARCHAR(50) NULL;
	END

	IF COL_LENGTH('dbo.Users', 'PasswordHash') IS NULL
	BEGIN
		ALTER TABLE dbo.Users ADD PasswordHash NVARCHAR(256) NULL;
	END

	-- create unique filtered index if not exists
	IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_Username' AND object_id = OBJECT_ID('dbo.Users'))
	BEGIN
		EXEC('CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username) WHERE Username IS NOT NULL');
	END
END
GO

IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
BEGIN
	CREATE TABLE dbo.Transactions (
		TransactionId INT IDENTITY PRIMARY KEY,
		UserId INT NOT NULL,
		Type NVARCHAR(20) NOT NULL,
		CurrencyCode NVARCHAR(3) NOT NULL,
		Amount DECIMAL(18,2) NOT NULL,
		Rate DECIMAL(18,4) NOT NULL,
		PlnAmount DECIMAL(18,2) NOT NULL,
		CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
		CONSTRAINT FK_Transactions_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId) ON DELETE CASCADE
	);
END
GO
