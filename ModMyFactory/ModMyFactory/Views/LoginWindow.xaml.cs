using System.Windows;

namespace ModMyFactory.Views
{
    partial class LoginWindow
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Switches the window to only ask for the two-factor authentication code,
        /// hiding the credential fields (used when credentials are already known and valid).
        /// </summary>
        public void SetAuthenticationCodeMode()
        {
            UsernameHeader.Visibility = Visibility.Collapsed;
            UsernameBox.Visibility = Visibility.Collapsed;
            PasswordHeader.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Collapsed;
            SaveCredentialsBox.Visibility = Visibility.Collapsed;

            AuthCodePanel.Visibility = Visibility.Visible;
            AuthCodeBox.Focus();
        }

        private void OKButtonClickHandler(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
