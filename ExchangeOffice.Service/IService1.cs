using System.ServiceModel;

namespace ExchangeOffice.Service
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
    }
}