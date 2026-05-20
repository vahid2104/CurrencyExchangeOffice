using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows;
using System.Windows.Controls;
using ExchangeServiceReference;

namespace ExchangeOffice.WpfClient
{
    public partial class DashboardWindow : Window
    {
        private readonly string _serviceAddress;
        private readonly int _currentUserId;
        private readonly string _currentUsername;
        private IService1 _channel;

        public DashboardWindow(int userId, string username, string serviceAddress)
        {
            InitializeComponent();
            _currentUserId = userId;
            _currentUsername = username;
            _serviceAddress = serviceAddress;
            TxtLoggedInUser.Text = $"Logged in as: {_currentUsername}";
            InitializeChannel();
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

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            // Close dashboard which will reveal auth window
            this.Close();
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

        private void BtnTopUp_Click(object sender, RoutedEventArgs e)
        {
            var currency = (CmbTopUpCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "PLN";
            if (!decimal.TryParse(TxtTopUpAmount.Text, out var amount))
            {
                MessageBox.Show("Invalid amount.");
                return;
            }

            try
            {
                var newBal = _channel.TopUpBalance(_currentUserId, currency, amount);
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

        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
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
                var bal = _channel.BuyCurrency(_currentUserId, currency, amount);
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
                var pln = _channel.SellCurrency(_currentUserId, currency, amount);
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
            try
            {
                var balances = _channel.GetUserBalances(_currentUserId);
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
            try
            {
                var history = _channel.GetTransactionHistory(_currentUserId);
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
