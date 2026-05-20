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

        [OperationContract]
        int RegisterUser(string fullName, string username, string password);

        [OperationContract]
        int LoginUser(string username, string password);
    }

    // Lightweight client proxy using ClientBase<T>
    public class Service1Client : ClientBase<IService1>, IService1
    {
        public Service1Client(Binding binding, EndpointAddress remoteAddress) : base(binding, remoteAddress) { }

        public string GetServiceStatus() => Channel.GetServiceStatus();
        public decimal Add(decimal firstNumber, decimal secondNumber) => Channel.Add(firstNumber, secondNumber);
        public decimal GetCurrentExchangeRate(string currencyCode) => Channel.GetCurrentExchangeRate(currencyCode);
        public int CreateUser(string fullName) => Channel.CreateUser(fullName);
        public decimal TopUpBalance(int userId, string currencyCode, decimal amount) => Channel.TopUpBalance(userId, currencyCode, amount);
        public string GetUserBalances(int userId) => Channel.GetUserBalances(userId);
        public decimal BuyCurrency(int userId, string currencyCode, decimal foreignAmount) => Channel.BuyCurrency(userId, currencyCode, foreignAmount);
        public decimal SellCurrency(int userId, string currencyCode, decimal foreignAmount) => Channel.SellCurrency(userId, currencyCode, foreignAmount);
        public string GetTransactionHistory(int userId) => Channel.GetTransactionHistory(userId);
        public string GetHistoricalExchangeRates(string currencyCode, string startDate, string endDate) => Channel.GetHistoricalExchangeRates(currencyCode, startDate, endDate);
        public int RegisterUser(string fullName, string username, string password) => Channel.RegisterUser(fullName, username, password);
        public int LoginUser(string username, string password) => Channel.LoginUser(username, password);
    }
}
