using MaterialDesignThemes.Wpf;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Auth;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class ProfilePage : Page
    {
        Frame mainFrame;
        private string originalFirstName;
        private string originalLastName;
        public ProfilePage(Frame _mainFrame)
        {
            InitializeComponent();
            this.mainFrame = _mainFrame;
            RefreshAll();
        }
        public async void RefreshAll()
        {
            LoadOrders();
            LoadMyProducts();
            LoadMyReviews();
            LoadUserData();
        }



        public async void LoadOrders()
        {
            try
            {
                var orders = await ApiHelper.GetAsync<List<OrderViewModel>>("Orders/My");

                foreach (var order in orders)
                {
                    order.ProductImages = order.ProductImages
                        .Select(img => $"{ApiHelper.BaseImagesUrl}{img}")
                        .ToList();
                }

                OrdersItemsControl.ItemsSource = orders;
            }
            catch (Exception ex)
            {
                
            }
        }

        public async void LoadMyProducts()
        {
            try
            {
                var products = await ApiHelper.GetAsync<List<MyProductViewModel>>("Orders/my-products");

                foreach (var product in products)
                {
                    if (!string.IsNullOrEmpty(product.MainImageUrl))
                        product.MainImageUrl = $"{ApiHelper.BaseImagesUrl}{product.MainImageUrl}";
                }

                MyProductsItemsControl.ItemsSource = products;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить товары: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async void LoadMyReviews()
        {
            try
            {
                var reviews = await ApiHelper.GetAsync<List<MyReviewViewModel>>("Products/my-reviews");
                // Префикс для картинок
                foreach (var review in reviews)
                {
                    review.ProductImageUrl = $"{ApiHelper.BaseImagesUrl}{review.ProductImageUrl}";
                }
                MyReviewsItemsControl.ItemsSource = reviews;
            }
            catch (Exception ex)
            {
                
            }
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && (border.DataContext is OrderViewModel order))
            {
                mainFrame.Navigate(new OrderDetailsPage(mainFrame,order.OrderId));
            }
        }

        private void BorderReviews_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && (border.DataContext is MyReviewViewModel review))
            {
                mainFrame.Navigate(new ProductDetailPage(review.ProductId, mainFrame));
            }
        }

        public async void LoadUserData()
        {
            try
            {
                var profile = await ApiHelper.GetAsync<UserDto>("Users/Current");
                if (profile != null)
                {
                    originalFirstName = profile.FirstName;
                    originalLastName = profile.LastName;

                    FirstNameTextBox.Text = originalFirstName;
                    LastNameTextBox.Text = originalLastName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить данные пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text) || FirstNameTextBox.Text.Length < 2)
                {
                    MessageBox.Show("Имя должно содержать минимум 2 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(LastNameTextBox.Text) || LastNameTextBox.Text.Length < 2)
                {
                    MessageBox.Show("Фамилия должна содержать минимум 2 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var dto = new UserDto
                {
                    FirstName = FirstNameTextBox.Text.Trim(),
                    LastName = LastNameTextBox.Text.Trim()
                };

                await ApiHelper.PutAsync("Users/update-profile", dto);

                originalFirstName = dto.FirstName;
                originalLastName = dto.LastName;

                SaveProfileButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении профиля: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FormatRussianName(TextBox textBox)
        {
            if (textBox == null) return;

            string text = new string(textBox.Text.Where(c => (c >= 'А' && c <= 'Я') || (c >= 'а' && c <= 'я')).ToArray());

            if (string.IsNullOrEmpty(text))
            {
                textBox.Text = "";
                return;
            }

            text = char.ToUpper(text[0]) + text.Substring(1).ToLower();

            if (textBox.Text != text)
            {
                int caret = textBox.CaretIndex;
                textBox.Text = text;
                textBox.CaretIndex = Math.Min(caret, text.Length);
            }
        }

        private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatRussianName(FirstNameTextBox);

            if (FirstNameTextBox == null || LastNameTextBox == null) return;

            SaveProfileButton.IsEnabled =
                FirstNameTextBox.Text.Trim() != originalFirstName ||
                LastNameTextBox.Text.Trim() != originalLastName;
        }

        private void LastNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatRussianName(FirstNameTextBox);

            if (FirstNameTextBox == null || LastNameTextBox == null) return;

            SaveProfileButton.IsEnabled =
                FirstNameTextBox.Text.Trim() != originalFirstName ||
                LastNameTextBox.Text.Trim() != originalLastName;
        }

        private async void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
                button.IsEnabled = false;

            try
            {
                var profile = await ApiHelper.GetAsync<UserDto>("Users/Current");
                if (profile == null)
                {
                    MessageBox.Show("Не удалось загрузить данные пользователя.", "Ошибка");
                    return;
                }

                string currentUserEmail = SettingsService.GetEmailFromToken();

                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    MessageBox.Show("Не удалось получить email пользователя.", "Ошибка");
                    button.IsEnabled = true;
                    return;
                }

                var response = await ApiHelper.PostAsync("Auth/send-reset-code", new
                {
                    email = currentUserEmail
                });

                var root = JsonDocument.Parse(response).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    button.IsEnabled = true;
                    return;
                }
                else
                {
                    MessageBox.Show("Код отправлен на вашу почту.");
                    ChangePasswordPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке запроса: {ex.Message}");
                button.IsEnabled = true;
            }
        }

        private async void ConfirmPasswordChangeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var profile = await ApiHelper.GetAsync<UserDto>("Users/Current");
                if (profile == null)
                {
                    MessageBox.Show("Не удалось получить email пользователя.", "Ошибка");
                    return;
                }

                string newPassword = NewPasswordBox.Password.Trim();
                string code = CodeTextBox.Text.Trim();

                if (newPassword.Length < 8 ||
                    !newPassword.Any(char.IsUpper) ||
                    !newPassword.Any(char.IsLower))
                {
                    MessageBox.Show("Пароль должен содержать минимум 8 символов, 1 строчную и 1 заглавную букву (англ).", "Ошибка");
                    return;
                }

                if (code.Length != 6 || !code.All(char.IsDigit))
                {
                    MessageBox.Show("Код подтверждения должен содержать 6 цифр.", "Ошибка");
                    return;
                }

                string currentUserEmail = SettingsService.GetEmailFromToken();

                var verifyResponse = await ApiHelper.PostAsync("Auth/verify-reset-code", new
                {
                    Email = currentUserEmail,
                    Code = code
                });

                var root = JsonDocument.Parse(verifyResponse).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var changeResponse = await ApiHelper.PostAsync("Auth/change-password", new
                {
                    Email = currentUserEmail,
                    NewPassword = newPassword
                });

                root = JsonDocument.Parse(changeResponse).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else
                {
                    MessageBox.Show("Пароль успешно изменён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    ChangePasswordPanel.Visibility = Visibility.Collapsed;
                    ChangePasswordButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при смене пароля: {ex.Message}");
            }
        }

        private void CodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CodeTextBox == null) return;

            string text = new string(CodeTextBox.Text.Where(char.IsDigit).ToArray());

            if (CodeTextBox.Text != text)
            {
                int caret = CodeTextBox.CaretIndex;
                CodeTextBox.Text = text;
                CodeTextBox.CaretIndex = Math.Min(caret, text.Length);
            }
        }

        private async void Logout_ButtonClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
            "Вы действительно хотите выйти из аккаунта?",
            "Подтверждение выхода",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
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

                SettingsService.Settings.JwtToken = "";
                SettingsService.SaveSettings();

                var authWindow = new AuthWindow();
                authWindow.Show();

                Window.GetWindow(this)?.Close();
            }
        }

        private void BorderProduct_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && (border.DataContext is MyProductViewModel product))
            {
                mainFrame.Navigate(new ProductDetailPage(product.ProductId, mainFrame));
            }
        }

        private async void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MyProductViewModel product)
            {
                try
                {
                    if (product.IsInCart)
                    {
                        mainFrame.Navigate(new CartPage(mainFrame));
                    }
                    else
                    {
                        await ApiHelper.PostAsync($"Cart/{product.ProductId}", null);
                        product.IsInCart = true;

                        button.Content = product.CartButtonText;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка добавления в корзину: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    public class MyProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string MainImageUrl { get; set; }
        public decimal Price { get; set; }

        public bool IsInCart { get; set; }
        public string CartButtonText => IsInCart ? "В корзине" : "Купить снова";
    }

    public class MyReviewViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductImageUrl { get; set; }
        public string Comment { get; set; }
        public int Rating { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class OrderViewModel
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<string> ProductImages { get; set; } = new();
    }
}
