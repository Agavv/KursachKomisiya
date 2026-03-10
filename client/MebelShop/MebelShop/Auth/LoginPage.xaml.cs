using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Admin;
using MebelShop.Customer;
using MebelShop.Director;
using MebelShop.Helpers;
using MebelShop.Manager;

namespace MebelShop.Auth
{
    public partial class LoginPage : Page
    {
        private Frame _mainFrame;

        public LoginPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new RegisterPage(_mainFrame));
        }

        public async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text;
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/login", new { Email = email, Password = password });
                var jsonDoc = JsonDocument.Parse(responseJson);
                var root = jsonDoc.RootElement;

                string token = root.GetProperty("token").GetString();
                string role = root.GetProperty("role").GetString();

                SettingsService.Settings.JwtToken = token;
                SettingsService.SaveSettings();

                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

                if (!email.Equals("root", StringComparison.OrdinalIgnoreCase))
                {
                    var json = await ApiHelper.GetAsync<JsonElement>("Users/current-theme");
                        
                    if (json.TryGetProperty("theme", out var themeProp))
                    {
                        string serverTheme = themeProp.GetString() ?? "Light";
                        if (!string.Equals(SettingsService.Settings.Theme, serverTheme, StringComparison.OrdinalIgnoreCase))
                        {
                            SettingsService.ApplyTheme(serverTheme);
                        }
                    }
                }

                Window windowToOpen = userRole switch
                {
                    "Покупатель" => new CustomerWindow(),
                    "Администратор" => new AdminWindow(),
                    "Директор" => new DirectorWindow(),
                    "Менеджер" => new ManagerWindow(),
                    _ => null
                };

                if (windowToOpen != null)
                {
                    windowToOpen.Show();

                    Window authWindow = Window.GetWindow(this);
                    authWindow?.Close();
                }
                else
                {
                    MessageBox.Show("Неизвестная роль пользователя", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (HttpRequestException httpEx) when (httpEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                MessageBox.Show("Неверный логин или пароль", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка авторизации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new ResetPasswordPage(_mainFrame));
        }
    }
}
