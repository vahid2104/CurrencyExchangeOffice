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
		CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
	);
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
