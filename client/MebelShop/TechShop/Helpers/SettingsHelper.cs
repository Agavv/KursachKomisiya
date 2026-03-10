using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using MebelShop.Auth;
using MebelShop.Models;

namespace MebelShop.Helpers
{
    public static class SettingsService
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MebelShop",
            "settings.json");

        private static readonly PaletteHelper PaletteHelper = new PaletteHelper();

        public static AppSettings Settings { get; private set; }

        public static void LoadSettings()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                try
                {
                    Settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
                catch
                {
                    Settings = new AppSettings();
                    SaveSettings();
                }
            }
            else
            {
                Settings = new AppSettings();
                SaveSettings();
            }

            ApplyTheme(Settings.Theme);
        }

        public static void SaveSettings()
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }

        public static void ApplyTheme(string theme)
        {
            bool isDark = theme.Equals("Dark", StringComparison.OrdinalIgnoreCase);
            var currentTheme = PaletteHelper.GetTheme();
            currentTheme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            PaletteHelper.SetTheme(currentTheme);

            Settings.Theme = isDark ? "Dark" : "Light";
            SaveSettings();

            Application.Current.Resources["ThemeIcon"] = isDark ? "☀" : "🌙";
        }

        public static void ToggleTheme()
        {
            ApplyTheme(IsDarkTheme() ? "Light" : "Dark");
        }

        public static bool IsDarkTheme()
        {
            var theme = PaletteHelper.GetTheme();
            return theme.GetBaseTheme() == BaseTheme.Dark;
        }

        public static string? GetEmailFromToken()
        {
            if (string.IsNullOrWhiteSpace(Settings.JwtToken))
                return null;

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(Settings.JwtToken);

            foreach (var claim in jwt.Claims)
            {
                Console.WriteLine($"{claim.Type} = {claim.Value}");
            }

            var email = jwt.Claims.FirstOrDefault(c =>
                c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"
                || c.Type == JwtRegisteredClaimNames.Sub
                || c.Type == "name"
                || c.Type == "unique_name"
            )?.Value;

            return email;
        }

        public static async Task LogoutAsync(Window? currentWindow = null)
        {
            var result = MessageBox.Show(
                "Вы действительно хотите выйти из аккаунта?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            // Сохраняем тему, если это не root
            if (!string.IsNullOrWhiteSpace(Settings.JwtToken) && !IsRoot())
            {
                try
                {
                    await ApiHelper.PostAsync("Users/update-theme", new { Theme = Settings.Theme });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось сохранить тему: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            Settings.JwtToken = "";
            SaveSettings();

            // Открываем окно авторизации
            var authWindow = new AuthWindow();
            authWindow.Show();

            // Закрываем текущее окно, если передано
            currentWindow?.Close();
        }

        public static bool IsRoot()
        {
            if (string.IsNullOrWhiteSpace(Settings.JwtToken))
                return false;

            var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(Settings.JwtToken);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "IsEmergency")?.Value == "true";
        }
    }
}
