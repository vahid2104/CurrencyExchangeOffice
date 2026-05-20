# Project Report

Course: Network Application Development

Project: Currency Exchange Office System

Author: Vahid Aliyev

Student ID: 64284

## System architecture

The solution is built as a small distributed application with three main components:
- WCF SOAP service (ExchangeOffice.Service) — business logic and data access.
- Clients (ExchangeOffice.WpfClient and ExchangeOffice.ConsoleClient) — user interaction and testing.
- SQL Server LocalDB — persistent storage for users, balances and transactions.

Clients call the WCF service over HTTP (BasicHttpBinding). The service communicates with the NBP public API to obtain current and historical exchange rates and uses ADO.NET to persist data in LocalDB.

## Roles

- WCF service role
  - Exposes operations for account management, balance updates, currency buy/sell, and rate retrieval.
  - Coordinates database transactions for balance and transaction records.
  - Calls the NBP API for exchange rate data.

- WPF client role
  - Provides a simple GUI to exercise service operations: create user, top up, buy/sell, view balances and history, and request rates.
  - Uses a lightweight client proxy to call the service.

- Console client role
  - A simple test client used to demonstrate and manually test service operations from the command line.

- SQL Server LocalDB role
  - Stores Users, Balances and Transactions tables.
  - Ensures transactional integrity when modifying balances and recording transactions.

- NBP API integration
  - The service fetches current exchange rates and historical series from the National Bank of Poland public API (JSON responses) and parses them into service responses.

## Implemented features

- Service status check
- Create user accounts
- Top up virtual balances (PLN and supported currencies)
- Retrieve current exchange rate for supported currencies
- Retrieve historical exchange rates for a date range
- Buy foreign currency using PLN (updates balances and records transaction)
- Sell foreign currency for PLN (updates balances and records transaction)
- View user balances
- View transaction history

## Database tables

- dbo.Users
  - UserId (identity), FullName

- dbo.Balances
  - Balance rows per (UserId, CurrencyCode), Amount
  - Intended unique pair: (UserId, CurrencyCode)

- dbo.Transactions
  - TransactionId (identity), UserId, Type, CurrencyCode, Amount, Rate, PlnAmount, CreatedAt

## Demo / testing scenario

1. Create a user via the WPF client or console client and note the returned user id.
2. Top up 1000 PLN using TopUpBalance.
3. Request current USD and EUR rates via GetCurrentExchangeRate.
4. Buy 10 USD using BuyCurrency.
5. Sell 5 USD using SellCurrency.
6. Display balances with GetUserBalances to verify results.
7. Display transactions with GetTransactionHistory.
8. Request historical rates with GetHistoricalExchangeRates for a chosen date range.

## Limitations

- Educational sample: no real payments or external account verification.
- Requires internet access for NBP API calls; NBP may not return rates for weekends or non-business days.
- SQL Server LocalDB must be installed and the provided SQL script must be executed to create the database schema before running the service.
- Endpoints in sample clients are configured for localhost and may need adjustment for different environments.

---

This report describes the implemented functionality present in the repository at the time of writing. No source code changes were made to produce this document.