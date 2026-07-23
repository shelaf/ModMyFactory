using System;
using System.ComponentModel;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using ModMyFactory.Helpers;
using ModMyFactory.Views;
using ModMyFactory.Web;
using ModMyFactory.Web.AuthenticationApi;
using Newtonsoft.Json;
using WPFCore;

namespace ModMyFactory
{
    sealed class GlobalCredentials : NotifyPropertyChangedBase
    {
        [JsonObject(MemberSerialization.OptOut)]
        private struct CredentialsExportTemplate
        {
            public string Entropy;
            public string ProtectedUsername;
            public string ProtectedPassword;
        }


        public static GlobalCredentials Instance { get; }

        private static FileInfo CredentialsFile { get; }

        static GlobalCredentials()
        {
            CredentialsFile = new FileInfo(Path.Combine(App.Instance.AppDataPath, "credentials.json"));
            Instance = new GlobalCredentials();
        }

        private static byte[] GenerateEntropy()
        {
            byte[] entropy = new byte[64];

            var cryptoServiceProvider = new RNGCryptoServiceProvider();
            cryptoServiceProvider.GetBytes(entropy);

            return entropy;
        }


        string username;
        SecureString password;
        string token;

        public string Username
        {
            get { return username; }
            set
            {
                if (value != username)
                {
                    username = value;
                    token = null;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(Username)));
                }
            }
        }

        public SecureString Password
        {
            get { return password; }
            set
            {
                password = value;
                token = null;
                OnPropertyChanged(new PropertyChangedEventArgs(nameof(Password)));
            }
        }

        private void Load(FileInfo file)
        {
            CredentialsExportTemplate template = JsonHelper.Deserialize<CredentialsExportTemplate>(file);

            byte[] entropy = Convert.FromBase64String(template.Entropy);
            byte[] protectedUsernameBytes = Convert.FromBase64String(template.ProtectedUsername);
            byte[] protectedPasswordBytes = Convert.FromBase64String(template.ProtectedPassword);

            byte[] usernameBytes = ProtectedData.Unprotect(protectedUsernameBytes, entropy, DataProtectionScope.CurrentUser);
            byte[] passwordBytes = ProtectedData.Unprotect(protectedPasswordBytes, entropy, DataProtectionScope.CurrentUser);

            try
            {
                username = Encoding.Unicode.GetString(usernameBytes);
                password = SecureStringHelper.SecureStringFromBytes(passwordBytes, Encoding.Unicode);
            }
            finally
            {
                SecureStringHelper.DestroySecureByteArray(passwordBytes);
            }
        }

        private GlobalCredentials()
        {
            token = null;

            if (App.Instance.Settings.SaveCredentials)
            {
                try
                {
                    if (CredentialsFile.Exists) Load(CredentialsFile);
                }
                catch (CryptographicException)
                {
                    DeleteSave();
                }
            }
            else
            {
                DeleteSave();
            }
        }

        private bool IsLoggedIn() => !string.IsNullOrEmpty(Username) && (Password != null);

        private bool IsLoggedInWithToken() => IsLoggedIn() && !string.IsNullOrEmpty(token);

        public bool LogIn(Window owner, out string loginToken)
        {
            loginToken = null;

            bool failed = false;
            if (IsLoggedInWithToken()) // Credentials and token available.
            {
                // Token only expires on user request, best to not check and save bandwidth.
            }
            else if (IsLoggedIn()) // Only credentials available.
            {
                if (!TryAuthenticate(owner, username, password, App.Instance.Settings.SaveCredentials))
                    failed = true;
            }

            if (failed)
            {
                token = null;
            }

            while (!IsLoggedInWithToken())
            {
                var loginWindow = new LoginWindow
                {
                    Owner = owner,
                    SaveCredentialsBox = { IsChecked = App.Instance.Settings.SaveCredentials },
                    FailedText = { Visibility = failed ? Visibility.Visible : Visibility.Collapsed },
                };
                bool? loginResult = loginWindow.ShowDialog();
                if (loginResult == null || loginResult == false) return false;

                string enteredUsername = loginWindow.UsernameBox.Text;
                SecureString enteredPassword = loginWindow.PasswordBox.SecurePassword;

                bool saveCredentials = loginWindow.SaveCredentialsBox.IsChecked ?? false;
                App.Instance.Settings.SaveCredentials = saveCredentials;

                failed = !TryAuthenticate(owner, enteredUsername, enteredPassword, saveCredentials);
            }

            loginToken = token;
            return true;
        }

        /// <summary>
        /// Attempts to authenticate with the given credentials, resolving two-factor authentication if required.
        /// On success the credentials and token are stored (and persisted when requested).
        /// </summary>
        private bool TryAuthenticate(Window owner, string user, SecureString pass, bool saveCredentials)
        {
            var result = ApiAuthentication.LogIn(user, pass, null, out var info);

            while (result == AuthenticationResult.EmailAuthenticationRequired)
            {
                string code = PromptAuthenticationCode(owner);
                if (code == null) return false; // User cancelled.

                result = ApiAuthentication.LogIn(user, pass, code, out info);
            }

            if (result != AuthenticationResult.Success)
            {
                token = null;
                return false;
            }

            username = info.Username;
            password = pass;
            token = info.Token;
            if (saveCredentials && App.Instance.Settings.SaveCredentials) Save();

            return true;
        }

        private static string PromptAuthenticationCode(Window owner)
        {
            var window = new LoginWindow { Owner = owner };
            window.SetAuthenticationCodeMode();

            bool? result = window.ShowDialog();
            if (result == null || result == false) return null;

            return window.AuthCodeBox.Text;
        }

        private void Save(FileInfo file)
        {
            byte[] usernameBytes = Encoding.Unicode.GetBytes(Username);
            byte[] passwordBytes = new byte[SecureStringHelper.GetSecureStringByteCount(Password, Encoding.Unicode)];
            SecureStringHelper.SecureStringToBytes(Password, passwordBytes, 0, Encoding.Unicode);

            try
            {
                byte[] entropy = GenerateEntropy();
                byte[] protectedUsernameBytes = ProtectedData.Protect(usernameBytes, entropy, DataProtectionScope.CurrentUser);
                byte[] protectedPasswordBytes = ProtectedData.Protect(passwordBytes, entropy, DataProtectionScope.CurrentUser);

                var template = new CredentialsExportTemplate()
                {
                    Entropy = Convert.ToBase64String(entropy),
                    ProtectedUsername = Convert.ToBase64String(protectedUsernameBytes),
                    ProtectedPassword = Convert.ToBase64String(protectedPasswordBytes),
                };

                JsonHelper.Serialize(template, file);
            }
            finally
            {
                SecureStringHelper.DestroySecureByteArray(passwordBytes);
            }
        }

        public void Save()
        {
            Save(CredentialsFile);
        }

        public void DeleteSave()
        {
            if (CredentialsFile.Exists) CredentialsFile.Delete();
        }
    }
}
