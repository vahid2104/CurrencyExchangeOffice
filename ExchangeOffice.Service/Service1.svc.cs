using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.ServiceModel;
using System.Collections.Generic;
using System.Linq;

namespace ExchangeOffice.Service
{
    public class Service1 : IService1
    {
        // Simple in-memory storage
        private static readonly object _lock = new object();
        private static readonly Dictionary<int, UserAccount> _users = new Dictionary<int, UserAccount>();
        private static int _nextUserId = 1;
        // Transactions in-memory
        private static readonly List<Transaction> _transactions = new List<Transaction>();
        private static int _nextTransactionId = 1;

        // Supported currencies - PLN is base
        private static readonly HashSet<string> _supportedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "PLN","USD","EUR","GBP","CHF"
        };

        public string GetServiceStatus()
        {
            return $"Exchange Office WCF Service is running. Server time: {DateTime.Now}";
        }

        public decimal Add(decimal firstNumber, decimal secondNumber)
        {
            return firstNumber + secondNumber;
        }

        public decimal GetCurrentExchangeRate(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                throw new FaultException("Currency code must be provided.");
            }

            var code = currencyCode.Trim().ToLowerInvariant();

            var url = $"http://api.nbp.pl/api/exchangerates/rates/a/{code}/?format=json";

            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    var json = client.DownloadString(url);

                    var serializer = new DataContractJsonSerializer(typeof(NbpExchangeRateResponse));
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(json)))
                    {
                        var response = (NbpExchangeRateResponse)serializer.ReadObject(ms);
                        if (response == null || response.rates == null || response.rates.Length == 0)
                        {
                            throw new FaultException($"No exchange rate found for '{currencyCode}'.");
                        }

                        return response.rates[0].mid;
                    }
                }
            }
            catch (WebException wex)
            {
                throw new FaultException($"Error contacting NBP API: {wex.Message}");
            }
            catch (SerializationException sex)
            {
                throw new FaultException($"Error parsing NBP response: {sex.Message}");
            }
        }

        public int CreateUser(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new FaultException("Full name must be provided.");

            lock (_lock)
            {
                var user = new UserAccount
                {
                    UserId = _nextUserId++,
                    FullName = fullName.Trim(),
                    Balances = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "PLN", 0m }
                    }
                };

                _users[user.UserId] = user;
                return user.UserId;
            }
        }

        public decimal TopUpBalance(int userId, string currencyCode, decimal amount)
        {
            currencyCode = (currencyCode ?? "PLN").Trim().ToUpperInvariant();

            if (amount <= 0)
                throw new FaultException("Amount must be greater than zero.");

            lock (_lock)
            {
                if (!_users.TryGetValue(userId, out var user))
                    throw new FaultException($"User with id {userId} does not exist.");

                if (!_supportedCurrencies.Contains(currencyCode))
                    throw new FaultException($"Currency '{currencyCode}' is not supported.");

                if (!user.Balances.ContainsKey(currencyCode))
                    user.Balances[currencyCode] = 0m;

                user.Balances[currencyCode] += amount;

                // Record transaction
                var txn = new Transaction
                {
                    TransactionId = _nextTransactionId++,
                    UserId = userId,
                    Type = "TOP_UP",
                    CurrencyCode = currencyCode,
                    Amount = amount,
                    Rate = currencyCode == "PLN" ? 1m : 0m,
                    PlnAmount = currencyCode == "PLN" ? decimal.Round(amount, 2) : 0m,
                    CreatedAt = DateTime.UtcNow
                };
                _transactions.Add(txn);

                return user.Balances[currencyCode];
            }
        }

        public string GetUserBalances(int userId)
        {
            lock (_lock)
            {
                if (!_users.TryGetValue(userId, out var user))
                    throw new FaultException($"User with id {userId} does not exist.");

                // Ensure all supported currencies appear
                foreach (var c in _supportedCurrencies)
                {
                    if (!user.Balances.ContainsKey(c))
                        user.Balances[c] = 0m;
                }

                var sb = new StringBuilder();
                foreach (var c in user.Balances.Keys.OrderBy(k => k))
                {
                    sb.AppendLine($"{c}: {user.Balances[c]:0.00}");
                }

                return sb.ToString().TrimEnd();
            }
        }

        public decimal BuyCurrency(int userId, string currencyCode, decimal foreignAmount)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                throw new FaultException("Currency code must be provided.");

            if (foreignAmount <= 0)
                throw new FaultException("Foreign amount must be greater than zero.");

            currencyCode = currencyCode.Trim().ToUpperInvariant();

            if (currencyCode == "PLN")
                throw new FaultException("Buying PLN with PLN is not supported.");

            if (!_supportedCurrencies.Contains(currencyCode))
                throw new FaultException($"Currency '{currencyCode}' is not supported.");

            var rate = GetCurrentExchangeRate(currencyCode);
            var requiredPln = decimal.Round(foreignAmount * rate, 2);

            lock (_lock)
            {
                if (!_users.TryGetValue(userId, out var user))
                    throw new FaultException($"User with id {userId} does not exist.");

                if (!user.Balances.TryGetValue("PLN", out var plnBalance))
                    plnBalance = 0m;

                if (plnBalance < requiredPln)
                    throw new FaultException($"Insufficient PLN balance. Required: {requiredPln:0.00}, Available: {plnBalance:0.00}");

                // subtract PLN
                user.Balances["PLN"] = plnBalance - requiredPln;

                if (!user.Balances.ContainsKey(currencyCode))
                    user.Balances[currencyCode] = 0m;

                user.Balances[currencyCode] += foreignAmount;

                // Record transaction
                var buyTxn = new Transaction
                {
                    TransactionId = _nextTransactionId++,
                    UserId = userId,
                    Type = "BUY",
                    CurrencyCode = currencyCode,
                    Amount = foreignAmount,
                    Rate = rate,
                    PlnAmount = requiredPln,
                    CreatedAt = DateTime.UtcNow
                };
                _transactions.Add(buyTxn);

                return user.Balances[currencyCode];
            }
        }

        public decimal SellCurrency(int userId, string currencyCode, decimal foreignAmount)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                throw new FaultException("Currency code must be provided.");

            if (foreignAmount <= 0)
                throw new FaultException("Foreign amount must be greater than zero.");

            currencyCode = currencyCode.Trim().ToUpperInvariant();

            if (currencyCode == "PLN")
                throw new FaultException("Selling PLN is not supported.");

            if (!_supportedCurrencies.Contains(currencyCode))
                throw new FaultException($"Currency '{currencyCode}' is not supported.");

            var rate = GetCurrentExchangeRate(currencyCode);
            var plnToCredit = decimal.Round(foreignAmount * rate, 2);

            lock (_lock)
            {
                if (!_users.TryGetValue(userId, out var user))
                    throw new FaultException($"User with id {userId} does not exist.");

                if (!user.Balances.TryGetValue(currencyCode, out var currBalance))
                    currBalance = 0m;

                if (currBalance < foreignAmount)
                    throw new FaultException($"Insufficient {currencyCode} balance. Requested: {foreignAmount:0.00}, Available: {currBalance:0.00}");

                user.Balances[currencyCode] = currBalance - foreignAmount;

                if (!user.Balances.ContainsKey("PLN"))
                    user.Balances["PLN"] = 0m;

                user.Balances["PLN"] += plnToCredit;
                // Record transaction
                var sellTxn = new Transaction
                {
                    TransactionId = _nextTransactionId++,
                    UserId = userId,
                    Type = "SELL",
                    CurrencyCode = currencyCode,
                    Amount = foreignAmount,
                    Rate = rate,
                    PlnAmount = plnToCredit,
                    CreatedAt = DateTime.UtcNow
                };
                _transactions.Add(sellTxn);

                return user.Balances["PLN"];
            }
        }

        public string GetTransactionHistory(int userId)
        {
            lock (_lock)
            {
                if (!_users.ContainsKey(userId))
                    throw new FaultException($"User with id {userId} does not exist.");

                var userTxns = _transactions.Where(t => t.UserId == userId).OrderBy(t => t.TransactionId).ToList();
                if (userTxns.Count == 0)
                    return "No transactions found for this user.";

                var sb = new StringBuilder();
                foreach (var t in userTxns)
                {
                    sb.AppendLine($"[{t.CreatedAt:u}] Id:{t.TransactionId} Type:{t.Type} Currency:{t.CurrencyCode} Amount:{t.Amount:0.00} Rate:{t.Rate:0.00} PLN:{t.PlnAmount:0.00}");
                }

                return sb.ToString().TrimEnd();
            }
        }
    }

    [DataContract]
    public class NbpExchangeRateResponse
    {
        [DataMember]
        public string table { get; set; }

        [DataMember]
        public string currency { get; set; }

        [DataMember]
        public string code { get; set; }

        [DataMember]
        public NbpRateItem[] rates { get; set; }
    }

    [DataContract]
    public class NbpRateItem
    {
        [DataMember]
        public string no { get; set; }

        [DataMember]
        public string effectiveDate { get; set; }

        [DataMember]
        public decimal mid { get; set; }
    }

    // Simple models for user accounts
    public class UserAccount
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public Dictionary<string, decimal> Balances { get; set; }
    }

    // Transaction model
    public class Transaction
    {
        public int TransactionId { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Amount { get; set; }
        public decimal Rate { get; set; }
        public decimal PlnAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}