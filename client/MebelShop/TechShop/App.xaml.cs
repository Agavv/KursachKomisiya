using MaterialDesignThemes.Wpf;
using System.Windows;
using MebelShop.Auth;
using MebelShop.Helpers;

namespace MebelShop
{
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SettingsService.LoadSettings();

            if (!string.IsNullOrWhiteSpace(SettingsService.Settings.JwtToken) && !isRoot())
            {
                try
                {
                    var json = await ApiHelper.GetAsync<System.Text.Json.JsonElement>("Users/current-theme");

                    if (json.TryGetProperty("theme", out var themeProp))
                    {
                        string serverTheme = themeProp.GetString() ?? "Light";

                        if (!string.Equals(SettingsService.Settings.Theme, serverTheme, StringComparison.OrdinalIgnoreCase))
                        {
                            SettingsService.ApplyTheme(serverTheme);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }

        private bool isRoot()
        {
            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(SettingsService.Settings.JwtToken);
            var isRoot = jwtToken.Claims.FirstOrDefault(c => c.Type == "IsEmergency")?.Value == "true";

            return isRoot;
        }
    }
}
