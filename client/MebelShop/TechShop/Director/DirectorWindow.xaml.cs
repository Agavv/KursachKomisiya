using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Admin;
using MebelShop.Helpers;

namespace MebelShop.Director
{
    public partial class DirectorWindow : Window
    {
        private readonly Dictionary<Type, Page> _pageCache = new();

        public DirectorWindow()
        {
            InitializeComponent();

            HotkeyHelper.AttachHotkey(this, Key.D1, ModifierKeys.Alt, () => ReportsButton_Click(null, null));
            HotkeyHelper.AttachHotkey(this, Key.D2, ModifierKeys.Alt, () => EmployeesButton_Click(null, null));
            HotkeyHelper.AttachHotkey(this, Key.D3, ModifierKeys.Alt, () => LogsButton_Click(null, null));

            HotkeyHelper.AttachHotkey(this, Key.Left, ModifierKeys.Alt, () =>
            {
                if (MainFrame.CanGoBack)
                    MainFrame.GoBack();
            });

            HotkeyHelper.AttachHotkey(this, Key.Right, ModifierKeys.Alt, () =>
            {
                if (MainFrame.CanGoForward)
                    MainFrame.GoForward();
            });

            this.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.F5)
                {
                    if (MainFrame.Content is EmployeesPage employeesPage)
                    {
                        _ = employeesPage.LoadUsersAsync();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is ReportsPage reportsPage)
                    {
                        reportsPage.LoadReports();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is LogsPage logsPage)
                    {
                        _ = logsPage.LoadLogs();
                        e.Handled = true;
                    }
                }
            };

            HotkeyHelper.AttachLogoutHotkey(this);
            HotkeyHelper.AttachThemeHotkey(this);

            MainFrame.Navigate(new ReportsPage(MainFrame));
        }

        private void NavigateOrUpdate<T>(Func<T> factory, Action<T> updateAction = null) where T : Page
        {
            if (!_pageCache.TryGetValue(typeof(T), out var page))
            {
                page = factory();
                _pageCache[typeof(T)] = page;
                MainFrame.Navigate(page);
            }
            else
            {
                updateAction?.Invoke((T)page);

                if (MainFrame.Content != page)
                    MainFrame.Navigate(page);
            }
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SettingsService.Settings.JwtToken) && !SettingsService.IsRoot())
            {
                try
                {
                    await ApiHelper.PostAsync("Users/update-theme", new { Theme = SettingsService.Settings.Theme });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Не удалось сохранить тему: {ex.Message}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }

        private void ReportsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new ReportsPage(MainFrame),
                page => page.LoadReports()
            );
        }

        private void EmployeesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new EmployeesPage(MainFrame),
                page => _ = page.LoadUsersAsync()
            );
        }

        private void LogsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new LogsPage(MainFrame),
                page => _ = page.LoadLogs()
            );
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingsService.LogoutAsync(this);
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
        }
    }
}
