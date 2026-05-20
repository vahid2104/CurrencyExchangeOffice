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

                // Demo new exchange office operations
                try
                {
                    Console.WriteLine("\n--- Exchange Office Demo ---");

                    // 1. Create user
                    int userId = channel.CreateUser("Test User");
                    Console.WriteLine($"Created user 'Test User' with id: {userId}");

                    // 2. Top up 1000 PLN
                    var newPln = channel.TopUpBalance(userId, "PLN", 1000m);
                    Console.WriteLine($"Topped up PLN: new PLN balance = {newPln:0.00}");

                    // 3. Print balances
                    Console.WriteLine("Balances after top-up:");
                    Console.WriteLine(channel.GetUserBalances(userId));

                    // 4. Show USD and EUR rates
                    var usd = channel.GetCurrentExchangeRate("USD");
                    var eur = channel.GetCurrentExchangeRate("EUR");
                    Console.WriteLine($"Rates: USD={usd:0.####}, EUR={eur:0.####}");

                    // 5. Buy 10 USD
                    var usdBalance = channel.BuyCurrency(userId, "USD", 10m);
                    Console.WriteLine($"Bought 10 USD, USD balance now: {usdBalance:0.00}");

                    // 6. Print balances
                    Console.WriteLine("Balances after buying USD:");
                    Console.WriteLine(channel.GetUserBalances(userId));

                    // 7. Sell 5 USD
                    var plnAfterSell = channel.SellCurrency(userId, "USD", 5m);
                    Console.WriteLine($"Sold 5 USD, PLN balance now: {plnAfterSell:0.00}");

                    // 8. Print balances
                    Console.WriteLine("Balances after selling USD:");
                    Console.WriteLine(channel.GetUserBalances(userId));
                }
                catch (FaultException fex)
                {
                    Console.WriteLine("Service fault: " + fex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in demo: " + ex.Message);
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
