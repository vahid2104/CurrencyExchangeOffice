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

                string status = channel.GetServiceStatus();
                Console.WriteLine("Service status: " + status);

                decimal result = channel.Add(10.50m, 20.25m);
                Console.WriteLine($"Add(10.50, 20.25) = {result}");

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

                try
                {
                    Console.WriteLine("\nHistorical USD rates 2026-01-01 to 2026-01-10:");
                    var hist = channel.GetHistoricalExchangeRates("USD", "2026-01-01", "2026-01-10");
                    Console.WriteLine(hist);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to get historical rates: " + ex.Message);
                }

                try
                {
                    Console.WriteLine("\n--- Exchange Office Demo ---");

                    int userId = channel.CreateUser("Test User");
                    Console.WriteLine($"Created user 'Test User' with id: {userId}");

                    var newPln = channel.TopUpBalance(userId, "PLN", 1000m);
                    Console.WriteLine($"Topped up PLN: new PLN balance = {newPln:0.00}");

                    Console.WriteLine("Balances after top-up:");
                    Console.WriteLine(channel.GetUserBalances(userId));

                    var usd = channel.GetCurrentExchangeRate("USD");
                    var eur = channel.GetCurrentExchangeRate("EUR");
                    Console.WriteLine($"Rates: USD={usd:0.####}, EUR={eur:0.####}");

                    var usdBalance = channel.BuyCurrency(userId, "USD", 10m);
                    Console.WriteLine($"Bought 10 USD, USD balance now: {usdBalance:0.00}");

                    Console.WriteLine("Balances after buying USD:");
                    Console.WriteLine(channel.GetUserBalances(userId));

                    var plnAfterSell = channel.SellCurrency(userId, "USD", 5m);
                    Console.WriteLine($"Sold 5 USD, PLN balance now: {plnAfterSell:0.00}");

                    Console.WriteLine("Balances after selling USD:");
                    Console.WriteLine(channel.GetUserBalances(userId));

                    Console.WriteLine("\nTransaction history:");
                    Console.WriteLine(channel.GetTransactionHistory(userId));
                }
                catch (FaultException fex)
                {
                    Console.WriteLine("Service fault: " + fex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in demo: " + ex.Message);
                }

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
