using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows;
using ExchangeServiceReference;

namespace ExchangeOffice.WpfClient
{
    public partial class MainWindow : Window
    {
        private readonly string _serviceAddress = "http://localhost:62312/Service1.svc";
        private IService1 _channel;

        public MainWindow()
        {
            InitializeComponent();
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
                TxtAuthStatus.Text = "Failed to create service channel: " + ex.Message;
            }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var user = TxtLoginUsername.Text?.Trim();
            var pass = PwdLogin.Password;
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                TxtAuthStatus.Text = "Enter username and password.";
                return;
            }

            try
            {
                var id = _channel.LoginUser(user, pass);
                OpenDashboard(id, user);
            }
            catch (FaultException fex)
            {
                TxtAuthStatus.Text = fex.Message;
            }
            catch (Exception ex)
            {
                TxtAuthStatus.Text = "Error: " + ex.Message;
            }
        }

        private void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            var name = TxtRegFullName.Text?.Trim();
            var user = TxtRegUsername.Text?.Trim();
            var pass = PwdReg.Password;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                TxtAuthStatus.Text = "Please fill name, username, and password.";
                return;
            }

            try
            {
                var id = _channel.RegisterUser(name, user, pass);
                OpenDashboard(id, user);
            }
            catch (FaultException fex)
            {
                TxtAuthStatus.Text = fex.Message;
            }
            catch (Exception ex)
            {
                TxtAuthStatus.Text = "Error: " + ex.Message;
            }
        }

        private void OpenDashboard(int userId, string username)
        {
            var dash = new DashboardWindow(userId, username, _serviceAddress);
            dash.Owner = this;
            dash.Show();
            this.Hide();
            dash.Closed += (s, e) =>
            {
                // On logout dashboard will close and we show this auth window again
                this.Show();
            };
        }
    }
}
