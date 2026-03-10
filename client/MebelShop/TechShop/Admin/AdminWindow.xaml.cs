using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Auth;
using MebelShop.Helpers;
using MebelShop.Manager;

namespace MebelShop.Admin
{
    public partial class AdminWindow : Window
    {
        private readonly Dictionary<Type, Page> _pageCache = new();

        public AdminWindow()
        {
            InitializeComponent();

            HotkeyHelper.AttachHotkey(this, Key.D1, ModifierKeys.Alt, () => CategoriesButton_Click(null, null));
            HotkeyHelper.AttachHotkey(this, Key.D2, ModifierKeys.Alt, () => ProductsButton_Click(null, null));
            HotkeyHelper.AttachHotkey(this, Key.D3, ModifierKeys.Alt, () => DatabaseButton_Click(null, null));

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
                    if (MainFrame.Content is CategoriesPage categoriesPage)
                    {
                        categoriesPage.LoadCategories();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is ProductsPage productsPage)
                    {
                        productsPage.LoadProducts();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is DatabasePage databasePage)
                    {
                        databasePage.LoadBackups();
                        e.Handled = true;
                    }
                }
            };

            HotkeyHelper.AttachLogoutHotkey(this);
            HotkeyHelper.AttachThemeHotkey(this);

            MainFrame.Navigate(new CategoriesPage(MainFrame));
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

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
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

        private void CategoriesButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new CategoriesPage(MainFrame),
                page => page.LoadCategories()
            );
        }

        private void ProductsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new ProductsPage(MainFrame),
                page => page.LoadProducts()
            );
        }

        private async void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            await SettingsService.LogoutAsync(this);
        }

        private void DatabaseButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new DatabasePage());
        }
    }
}
