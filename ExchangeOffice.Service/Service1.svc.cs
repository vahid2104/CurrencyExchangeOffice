using System;

namespace ExchangeOffice.Service
{
    public class Service1 : IService1
    {
        public string GetServiceStatus()
        {
            return $"Exchange Office WCF Service is running. Server time: {DateTime.Now}";
        }

        public decimal Add(decimal firstNumber, decimal secondNumber)
        {
            return firstNumber + secondNumber;
        }
    }
}