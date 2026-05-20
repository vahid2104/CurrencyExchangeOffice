# Currency Exchange Office System

Course: Network Application Development

Author: Vahid Aliyev

Student ID: 64284

## Project overview

This project implements a simple currency exchange office system for the Network Application Development course. It uses a WCF SOAP service (ExchangeOffice.Service) to provide currency exchange operations, a WPF desktop client (ExchangeOffice.WpfClient) and a console client (ExchangeOffice.ConsoleClient) for testing. Data is persisted in a SQL Server LocalDB database and current/historical exchange rates are retrieved from the National Bank of Poland (NBP) public API.

## Technologies

- .NET Framework 4.8
- WCF (SOAP)
- WPF
- Console application
- SQL Server LocalDB
- ADO.NET
- National Bank of Poland (NBP) API
- Visual Studio
- Git / GitHub

## Repository / solution structure

- ExchangeOffice.Service - WCF service and business logic
- ExchangeOffice.ConsoleClient - console test client
- ExchangeOffice.WpfClient - WPF desktop client
- Database/CreateDatabase.sql - SQL script to create schema and sample data
- README.md - this document

## Main features

- Check WCF service status
- Create user account
- Top up virtual balance
- Retrieve current exchange rates from NBP API
- Retrieve historical exchange rates from NBP API
- Buy foreign currency with PLN
- Sell foreign currency for PLN
- View user balances
- View transaction history
- Persist users, balances and transactions in SQL Server LocalDB

## Simple architecture

WPF Client / Console Client
		|
		v
WCF SOAP Service
		|
		+--> NBP API (rates)
		|
		+--> SQL Server LocalDB (persistence)

Components:
- WPF/Console clients call the WCF service over HTTP (BasicHttpBinding).
- The service queries NBP for rates and updates the LocalDB via ADO.NET.

## WCF service operations

- GetServiceStatus()
- Add(decimal firstNumber, decimal secondNumber)
- GetCurrentExchangeRate(string currencyCode)
- GetHistoricalExchangeRates(string currencyCode, string startDate, string endDate)
- CreateUser(string fullName)
- TopUpBalance(int userId, string currencyCode, decimal amount)
- GetUserBalances(int userId)
- BuyCurrency(int userId, string currencyCode, decimal foreignAmount)
- SellCurrency(int userId, string currencyCode, decimal foreignAmount)
- GetTransactionHistory(int userId)

All operations throw FaultException on failure and return primitive types or formatted strings for easy display in the sample clients.

## Database schema (summary)

- dbo.Users
  - UserId (PK, identity)
  - FullName

- dbo.Balances
  - BalanceId (PK)
  - UserId (FK -> dbo.Users.UserId)
  - CurrencyCode
  - Amount
  - Unique constraint on (UserId, CurrencyCode)

- dbo.Transactions
  - TransactionId (PK, identity)
  - UserId (FK -> dbo.Users.UserId)
  - Type (TOP_UP / BUY / SELL)
  - CurrencyCode
  - Amount
  - Rate
  - PlnAmount
  - CreatedAt

Relationships:
- Users 1..* Balances
- Users 1..* Transactions

## Database setup

1. Open the solution in Visual Studio.
2. Open SQL Server Object Explorer.
3. Connect to (localdb)\MSSQLLocalDB.
4. Open Database/CreateDatabase.sql and execute it against the (localdb) instance.
5. Confirm database `CurrencyExchangeOfficeDb` exists and contains tables dbo.Users, dbo.Balances, dbo.Transactions.

## How to run

1. Clone the repository and open the solution in Visual Studio (solution targets .NET Framework 4.8).
2. Build the solution (Restore and build in Visual Studio).
3. Create the LocalDB database using Database/CreateDatabase.sql as described above.
4. In Visual Studio set the startup projects (right-click solution -> Set Startup Projects):
   - Startup project 1: ExchangeOffice.Service (Start)
   - Startup project 2: ExchangeOffice.WpfClient (Start)
   - ExchangeOffice.ConsoleClient can be left as None (it is a simple test client).
5. Run the solution (Ctrl+F5). The WCF service should be hosted locally and the WPF client will connect to it.

Notes:
- The service uses BasicHttpBinding and a hard-coded localhost URL in the sample clients; adjust endpoints in client code or config if necessary.
- An internet connection is required for NBP API calls.

## Demo scenario

From the WPF client (or Console client) try the following sequence:
1. Create a user (enter a full name). Note the returned user id.
2. Top up 1000 PLN for the new user.
3. Read current USD/EUR rates (buttons in the WPF client).
4. Buy 10 USD using the Buy operation.
5. Sell 5 USD using the Sell operation.
6. Show balances to verify results.
7. Show transaction history to view recorded transactions.
8. Get historical USD rates for an example range (e.g. 2025-01-02 to 2025-01-10).

## Notes and limitations

- This project is an educational example. No real payments are performed.
- NBP API may not return rates for weekends or non-business days.
- SQL Server LocalDB must be installed and accessible on the machine running the service.

## Submission

This project is prepared for Moodle submission via a public GitHub repository link. Include this repository URL when submitting the assignment.

---

If you need a short walkthrough for creating the LocalDB database or adjusting the service endpoint, mention the target environment and I will provide concise steps.