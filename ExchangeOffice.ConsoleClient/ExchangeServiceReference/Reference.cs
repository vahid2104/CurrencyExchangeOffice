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
    }
}
