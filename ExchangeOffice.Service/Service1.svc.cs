using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.ServiceModel;

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
}