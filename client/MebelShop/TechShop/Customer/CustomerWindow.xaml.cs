using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Auth;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class CustomerWindow : Window
    {
        private readonly Dictionary<Type, Page> _pageCache = new();
        public CustomerWindow()
        {
            InitializeComponent();
            //выход
            HotkeyHelper.AttachLogoutHotkey(this);
            //тема
            HotkeyHelper.AttachThemeHotkey(this);

            //каталог
            HotkeyHelper.AttachHotkey(this, Key.D1, ModifierKeys.Alt, () => CatalogButton_Click(null, null));
            //корзина
            HotkeyHelper.AttachHotkey(this, Key.D2, ModifierKeys.Alt, () => CartButton_Click(null, null));
            //профиль
            HotkeyHelper.AttachHotkey(this, Key.D3, ModifierKeys.Alt, () => ProfileButton_Click(null, null));

            //назад
            HotkeyHelper.AttachHotkey(this, Key.Left, ModifierKeys.Alt, () =>
            {
                if (MainFrame.CanGoBack)
                    MainFrame.GoBack();
            });

            // вперед
            HotkeyHelper.AttachHotkey(this, Key.Right, ModifierKeys.Alt, () =>
            {
                if (MainFrame.CanGoForward)
                    MainFrame.GoForward();
            });

            this.PreviewKeyDown += (sender, e) =>
            {
                if (e.Key == Key.F5)
                {
                    if (MainFrame.Content is ProfilePage profilePage)
                    {
                        profilePage.RefreshAll();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is CatalogPage catalogPage)
                    {
                        catalogPage.CurrentPage = 1;
                        catalogPage.LoadProducts();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is CartPage cartPage)
                    {
                        cartPage.LoadCart();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is ProductDetailPage productDetailPage)
                    {
                        productDetailPage.LoadProductDetail();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is OrderDetailsPage orderDetailsPage)
                    {
                        orderDetailsPage.LoadOrderDetails();
                        e.Handled = true;
                    }
                }
            };

            MainFrame.Navigate(new CatalogPage(MainFrame));
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

        private void CatalogButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new CatalogPage(MainFrame),
                page => page.LoadProducts()
            );
        }

        private void CartButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new CartPage(MainFrame),
                page => page.LoadCart()
            );
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new ProfilePage(MainFrame),
                page => page.RefreshAll()
            );
        }

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
        }

        private void MenuDarkModeButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsService.ToggleTheme();
        }

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(SettingsService.Settings.JwtToken))
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
    }
}
