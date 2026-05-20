using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using ExchangeServiceReference;

namespace ExchangeOffice.ConsoleClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceAddress = "http://localhost:62312/Service1.svc";

            ChannelFactory<IService1> factory = null;
            IService1 channel = null;
            IClientChannel clientChannel = null;

            try
            {
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress(serviceAddress);

                factory = new ChannelFactory<IService1>(binding, endpoint);
                channel = factory.CreateChannel();
                clientChannel = (IClientChannel)channel;

                // Call GetServiceStatus
                string status = channel.GetServiceStatus();
                Console.WriteLine("Service status: " + status);

                // Call Add
                decimal result = channel.Add(10.50m, 20.25m);
                Console.WriteLine($"Add(10.50, 20.25) = {result}");

                // Call GetCurrentExchangeRate for USD and EUR
                try
                {
                    decimal usdRate = channel.GetCurrentExchangeRate("USD");
                    Console.WriteLine($"Current USD rate (mid): {usdRate}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to get USD rate: " + ex.Message);
                }

                try
                {
                    decimal eurRate = channel.GetCurrentExchangeRate("EUR");
                    Console.WriteLine($"Current EUR rate (mid): {eurRate}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to get EUR rate: " + ex.Message);
                }

                // Close channel gracefully
                try
                {
                    clientChannel?.Close();
                    factory?.Close();
                }
                catch
                {
                    clientChannel?.Abort();
                    factory?.Abort();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calling service: " + ex.Message);
                try
                {
                    clientChannel?.Abort();
                    factory?.Abort();
                }
                catch { }
            }

            Console.WriteLine("Press ENTER to exit...");
            Console.ReadLine();
        }
    }
}
