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
        private readonly Data.ExchangeRepository _repo = new Data.ExchangeRepository();

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
            try
            {
                return _repo.CreateUser(fullName.Trim());
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error creating user: {ex.Message}");
            }
        }

        public decimal TopUpBalance(int userId, string currencyCode, decimal amount)
        {
            currencyCode = (currencyCode ?? "PLN").Trim().ToUpperInvariant();

            if (amount <= 0)
                throw new FaultException("Amount must be greater than zero.");
            if (!_supportedCurrencies.Contains(currencyCode))
                throw new FaultException($"Currency '{currencyCode}' is not supported.");

            try
            {
                return _repo.TopUpBalance(userId, currencyCode, amount);
            }
            catch (InvalidOperationException iox)
            {
                throw new FaultException(iox.Message);
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error topping up balance: {ex.Message}");
            }
        }

        public string GetUserBalances(int userId)
        {
            try
            {
                var balances = _repo.GetUserBalances(userId);
                // ensure all supported currencies present
                var dict = _supportedCurrencies.ToDictionary(c => c, c => 0m, StringComparer.OrdinalIgnoreCase);
                foreach (var b in balances)
                    dict[b.CurrencyCode] = b.Amount;

                var sb = new StringBuilder();
                foreach (var c in dict.Keys.OrderBy(k => k))
                    sb.AppendLine($"{c}: {dict[c]:0.00}");

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error reading balances: {ex.Message}");
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
            try
            {
                return _repo.BuyCurrency(userId, currencyCode, foreignAmount, rate);
            }
            catch (InvalidOperationException iox)
            {
                throw new FaultException(iox.Message);
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error executing buy: {ex.Message}");
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
            try
            {
                return _repo.SellCurrency(userId, currencyCode, foreignAmount, rate);
            }
            catch (InvalidOperationException iox)
            {
                throw new FaultException(iox.Message);
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error executing sell: {ex.Message}");
            }
        }

        public string GetTransactionHistory(int userId)
        {
            try
            {
                var txns = _repo.GetTransactionHistory(userId);
                if (txns.Count == 0)
                    return "No transactions found for this user.";

                var sb = new StringBuilder();
                foreach (var t in txns)
                {
                    sb.AppendLine($"[{t.CreatedAt:u}] Id:{t.TransactionId} Type:{t.Type} Currency:{t.CurrencyCode} Amount:{t.Amount:0.00} Rate:{t.Rate:0.00} PLN:{t.PlnAmount:0.00}");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception ex)
            {
                throw new FaultException($"Error reading transaction history: {ex.Message}");
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