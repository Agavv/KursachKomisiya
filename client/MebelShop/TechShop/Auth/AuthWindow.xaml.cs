using MaterialDesignThemes.Wpf;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Admin;
using MebelShop.Customer;
using MebelShop.Director;
using MebelShop.Helpers;
using MebelShop.Manager;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;

namespace MebelShop.Auth
{
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
            CheckExistingToken();

            HotkeyHelper.AttachThemeHotkey(this);
            MainFrame.Navigate(new LoginPage(MainFrame));

            this.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (MainFrame.Content is LoginPage loginPage)
                    {
                        loginPage.LoginButton_Click(null, null);
                        e.Handled = true;
                    }
                }
            };
        }

        private void CheckExistingToken()
        {
            string token = SettingsService.Settings.JwtToken;
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jwtToken = handler.ReadJwtToken(token);

                    var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
                    if (expClaim != null && long.TryParse(expClaim, out var exp))
                    {
                        var expiryDate = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                        if (expiryDate > DateTime.UtcNow)
                        {
                            var userRole = jwtToken.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

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
                                this.Close();
                                return;
                            }
                        }
                    }
                }
                catch
                {
                    
                }
            }

            SettingsService.Settings.JwtToken = "";
            SettingsService.SaveSettings();
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
        }
    }
}
