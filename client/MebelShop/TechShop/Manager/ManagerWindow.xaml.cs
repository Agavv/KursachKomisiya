using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Auth;
using MebelShop.Customer;
using MebelShop.Helpers;

namespace MebelShop.Manager
{
    public partial class ManagerWindow : Window
    {
        private readonly Dictionary<Type, Page> _pageCache = new();
        public ManagerWindow()
        {
            InitializeComponent();

            HotkeyHelper.AttachLogoutHotkey(this);
            HotkeyHelper.AttachThemeHotkey(this);

            //заказы
            HotkeyHelper.AttachHotkey(this, Key.D1, ModifierKeys.Alt, () => OrdersButton_Click(null, null));
            //отзывы
            HotkeyHelper.AttachHotkey(this, Key.D2, ModifierKeys.Alt, () => ReviewsButton_Click(null, null));

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
                    if (MainFrame.Content is OrdersPage ordersPage)
                    {
                        ordersPage.LoadOrders();
                        e.Handled = true;
                    }

                    if (MainFrame.Content is ReviewsPage reviewsPage)
                    {
                        reviewsPage.LoadReviews();
                        e.Handled = true;
                    }
                }
            };

            MainFrame.Navigate(new OrdersPage(MainFrame));
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

        private void OrdersButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new OrdersPage(MainFrame),
                page => page.LoadOrders()
            );
        }

        private void ReviewsButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateOrUpdate(
                () => new ReviewsPage(MainFrame),
                page => page.LoadReviews()
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
    }
}
