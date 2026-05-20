using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows;
using System.Windows.Controls;
// Lightweight manual proxy types embedded so the WPF project compiles without modifying the project file.

namespace ExchangeOffice.WpfClient
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

    // Simple ClientBase wrapper
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
    }

    public partial class MainWindow : Window
    {
        private readonly string _serviceAddress = "http://localhost:62312/Service1.svc";
        private IService1 _channel;

        public MainWindow()
        {
            InitializeComponent();
            InitializeChannel();
        }

        private void BtnHistoricalRates_Click(object sender, RoutedEventArgs e)
        {
            var currency = (CmbHistCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(currency))
            {
                MessageBox.Show("Select a currency.");
                return;
            }

            var start = TxtHistStart.Text?.Trim();
            var end = TxtHistEnd.Text?.Trim();
            if (string.IsNullOrEmpty(start) || string.IsNullOrEmpty(end))
            {
                MessageBox.Show("Please enter start and end dates in yyyy-MM-dd format.");
                return;
            }

            try
            {
                var result = _channel.GetHistoricalExchangeRates(currency, start, end);
                TxtOutput.Text = result;
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void InitializeChannel()
        {
            try
            {
                var binding = new BasicHttpBinding();
                var endpoint = new EndpointAddress(_serviceAddress);
                var factory = new ChannelFactory<IService1>(binding, endpoint);
                _channel = factory.CreateChannel();
            }
            catch (Exception ex)
            {
                TxtOutput.Text = "Failed to create service channel: " + ex.Message;
            }
        }

        private void BtnStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var status = _channel.GetServiceStatus();
                TxtStatus.Text = status;
            }
            catch (FaultException fex)
            {
                TxtStatus.Text = "Service fault: " + fex.Message;
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error: " + ex.Message;
            }
        }

        private void BtnCreateUser_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtFullName.Text?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Please enter a full name.");
                return;
            }

            try
            {
                int userId = _channel.CreateUser(name);
                TxtUserId.Text = userId.ToString();
                TxtOutput.Text = $"Created user '{name}' with id {userId}.";
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnTopUp_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTopUpUserId.Text, out var userId))
            {
                MessageBox.Show("Invalid user id.");
                return;
            }

            var currency = (CmbTopUpCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PLN";
            if (!decimal.TryParse(TxtTopUpAmount.Text, out var amount))
            {
                MessageBox.Show("Invalid amount.");
                return;
            }

            try
            {
                var newBal = _channel.TopUpBalance(userId, currency, amount);
                TxtOutput.Text = $"Top up successful. New {currency} balance: {newBal:0.00}";
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnUsdRate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rate = _channel.GetCurrentExchangeRate("USD");
                TxtRates.Text = $"USD: {rate:0.####}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting USD rate: " + ex.Message);
            }
        }

        private void BtnEurRate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var rate = _channel.GetCurrentExchangeRate("EUR");
                TxtRates.Text = $"EUR: {rate:0.####}";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error getting EUR rate: " + ex.Message);
            }
        }

        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTradeUserId.Text, out var userId))
            {
                MessageBox.Show("Invalid user id.");
                return;
            }

            var currency = (CmbTradeCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(currency))
            {
                MessageBox.Show("Select a currency.");
                return;
            }

            if (!decimal.TryParse(TxtTradeAmount.Text, out var amount))
            {
                MessageBox.Show("Invalid amount.");
                return;
            }

            try
            {
                var bal = _channel.BuyCurrency(userId, currency, amount);
                TxtOutput.Text = $"Bought {amount:0.00} {currency}. New {currency} balance: {bal:0.00}";
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnSell_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTradeUserId.Text, out var userId))
            {
                MessageBox.Show("Invalid user id.");
                return;
            }

            var currency = (CmbTradeCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(currency))
            {
                MessageBox.Show("Select a currency.");
                return;
            }

            if (!decimal.TryParse(TxtTradeAmount.Text, out var amount))
            {
                MessageBox.Show("Invalid amount.");
                return;
            }

            try
            {
                var pln = _channel.SellCurrency(userId, currency, amount);
                TxtOutput.Text = $"Sold {amount:0.00} {currency}. New PLN balance: {pln:0.00}";
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnShowBalances_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTopUpUserId.Text, out var userId))
            {
                MessageBox.Show("Invalid user id.");
                return;
            }

            try
            {
                var balances = _channel.GetUserBalances(userId);
                TxtOutput.Text = balances;
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnShowHistory_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(TxtTopUpUserId.Text, out var userId))
            {
                MessageBox.Show("Invalid user id.");
                return;
            }

            try
            {
                var history = _channel.GetTransactionHistory(userId);
                TxtOutput.Text = history;
            }
            catch (FaultException fex)
            {
                MessageBox.Show("Service fault: " + fex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }
    }
}
