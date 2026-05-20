using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ExchangeServiceReference
{
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        string GetServiceStatus();

        [OperationContract]
        decimal Add(decimal firstNumber, decimal secondNumber);

        [OperationContract]
        decimal GetCurrentExchangeRate(string currencyCode);

        [OperationContract]
        int CreateUser(string fullName);

        [OperationContract]
        decimal TopUpBalance(int userId, string currencyCode, decimal amount);

        [OperationContract]
        string GetUserBalances(int userId);

        [OperationContract]
        decimal BuyCurrency(int userId, string currencyCode, decimal foreignAmount);

        [OperationContract]
        decimal SellCurrency(int userId, string currencyCode, decimal foreignAmount);

        [OperationContract]
        string GetTransactionHistory(int userId);

        [OperationContract]
        string GetHistoricalExchangeRates(string currencyCode, string startDate, string endDate);
    }

    // Lightweight client proxy using ClientBase<T>
    public class Service1Client : ClientBase<IService1>, IService1
    {
        public Service1Client(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }

        public string GetServiceStatus()
        {
            return Channel.GetServiceStatus();
        }

        public decimal Add(decimal firstNumber, decimal secondNumber)
        {
            return Channel.Add(firstNumber, secondNumber);
        }

        public decimal GetCurrentExchangeRate(string currencyCode)
        {
            return Channel.GetCurrentExchangeRate(currencyCode);
        }

        public int CreateUser(string fullName)
        {
            return Channel.CreateUser(fullName);
        }

        public decimal TopUpBalance(int userId, string currencyCode, decimal amount)
        {
            return Channel.TopUpBalance(userId, currencyCode, amount);
        }

        public string GetUserBalances(int userId)
        {
            return Channel.GetUserBalances(userId);
        }

        public decimal BuyCurrency(int userId, string currencyCode, decimal foreignAmount)
        {
            return Channel.BuyCurrency(userId, currencyCode, foreignAmount);
        }

        public decimal SellCurrency(int userId, string currencyCode, decimal foreignAmount)
        {
            return Channel.SellCurrency(userId, currencyCode, foreignAmount);
        }

        public string GetTransactionHistory(int userId)
        {
            return Channel.GetTransactionHistory(userId);
        }

        public string GetHistoricalExchangeRates(string currencyCode, string startDate, string endDate)
        {
            return Channel.GetHistoricalExchangeRates(currencyCode, startDate, endDate);
        }
    }
}
