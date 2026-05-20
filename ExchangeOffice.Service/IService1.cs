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
    }
}