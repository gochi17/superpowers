using System.Windows;

namespace SuperpowersRevit.UI
{
    public partial class DrofusLoginWindow : Window
    {
        // Read by DrofusDataCommand after ShowDialog() returns true
        public string Host     { get; private set; } = string.Empty;
        public string Username { get; private set; } = string.Empty;
        public string Password { get; private set; } = string.Empty;

        public DrofusLoginWindow() => InitializeComponent();

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            HideError();

            string host = HostBox.Text.Trim();
            string user = UsernameBox.Text.Trim();
            string pass = PasswordBox.Password;

            if (string.IsNullOrEmpty(host))  { ShowError("Host URL is required.");  return; }
            if (string.IsNullOrEmpty(user))  { ShowError("Username is required.");  return; }
            if (string.IsNullOrEmpty(pass))  { ShowError("Password is required.");  return; }

            Host     = host;
            Username = user;
            Password = pass;

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) =>
            DialogResult = false;

        private void ShowError(string msg)
        {
            ErrorText.Text       = msg;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError() =>
            ErrorText.Visibility = Visibility.Collapsed;
    }
}
