using IOTrim;
using System.Windows;

namespace Delta_Riveting
{
    public partial class AdminLogin : Window
    {
        public bool IsAuthenticated { get; private set; } = false;
        public string EnteredUsername { get; private set; }
        public string EnteredPassword { get; private set; }
        public AdminLogin()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (!String.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                EnteredUsername = username;
                EnteredPassword = password;

                if(username == "Admin" && password == "admin123") 
                {
                    IsAuthenticated = true;
                    DialogResult = true;
                }
                else
                {
                    MessageBox.Show("Invalid username or password.", "Authentication Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter both username and password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
