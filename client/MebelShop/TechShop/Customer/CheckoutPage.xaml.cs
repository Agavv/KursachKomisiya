using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class CheckoutPage : Page
    {
        private Frame _mainFrame;
        private List<OrderItemViewModel> _orderItems = new List<OrderItemViewModel>();
        private UserPaymentDto _originalPayment;

        public CheckoutPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            PickupPointsComboBox.Items.Add("г. Москва, Нахимовский проспект, 21");
            PickupPointsComboBox.Items.Add("г. Москва, Нежинская улица, 7");

            RefreshAll();
        }

        public async void RefreshAll()
        {
            LoadUserData();
            LoadCartItems();
        }

        private async void LoadUserData()
        {
            try
            {
                // Получаем данные для оформления заказа
                var checkoutInfo = await ApiHelper.GetAsync<UserCheckoutDto>("Users/CheckoutInfo");
                if (checkoutInfo != null)
                {
                    // Имя и фамилия
                    FirstNameTextBox.Text = checkoutInfo.FirstName ?? "";
                    LastNameTextBox.Text = checkoutInfo.LastName ?? "";

                    // Способ доставки
                    if (checkoutInfo.DeliveryType == "Доставка по адресу")
                    {
                        // Включаем панель адреса
                        var deliveryRadio = DeliveryAddressPanel.Children.OfType<RadioButton>()
                            .FirstOrDefault(rb => rb.Content.ToString() == "Доставка по адресу");
                        if (deliveryRadio != null)
                            deliveryRadio.IsChecked = true;

                        AddressComboBox.Text = checkoutInfo.DeliveryAddress ?? "";
                    }
                    else
                    {
                        // Самовывоз
                        var pickupRadio = DeliveryAddressPanel.Children.OfType<RadioButton>()
                            .FirstOrDefault(rb => rb.Content.ToString() == "Самовывоз");
                        if (pickupRadio != null)
                            pickupRadio.IsChecked = true;

                        // Выбираем первый пункт самовывоза по умолчанию, если есть
                        if (!string.IsNullOrEmpty(checkoutInfo.DeliveryAddress))
                            PickupPointsComboBox.SelectedItem = checkoutInfo.DeliveryAddress;
                    }

                    CardNumberTextBox.Text = checkoutInfo.CardNumber ?? "";
                    CardExpiryTextBox.Text = checkoutInfo.Expiry ?? "";
                    CardCvvTextBox.Text = checkoutInfo.Cvv ?? "";

                    _originalPayment = new UserPaymentDto{
                        CardNumber = checkoutInfo.CardNumber ?? "",
                        Expiry = checkoutInfo.Expiry ?? "",
                        Cvv = checkoutInfo.Cvv ?? ""
                    };

                    // Способ оплаты
                    if (checkoutInfo.PaymentType != null && checkoutInfo.PaymentType.StartsWith("Онлайн"))
                    {
                        CardPaymentPanel.Visibility = Visibility.Visible;
                        CashOnDeliveryOptions.Visibility = Visibility.Collapsed;
                        OnlineRadio.IsChecked = true;
                        CashOnDeliveryRadio.IsChecked = false;
                    }
                    else
                    {
                        CardPaymentPanel.Visibility = Visibility.Collapsed;
                        CashOnDeliveryOptions.Visibility = Visibility.Visible;
                        CashOnDeliveryRadio.IsChecked = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить данные пользователя: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadCartItems()
        {
            try
            {
                var cartItems = await ApiHelper.GetAsync<List<CartItemViewModel>>("Cart");

                _orderItems = cartItems.Select(ci => new OrderItemViewModel
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.ProductName,
                    Price = ci.Price,
                    Quantity = ci.Quantity,
                    TotalPrice = ci.Price * ci.Quantity,
                    MainImageUrl = $"{ApiHelper.BaseImagesUrl}{ci.ImageUrl}"
                }).ToList();

                OrderItemsControl.ItemsSource = _orderItems;

                UpdateTotals();
            }
            catch (Exception ex)
            {
                
            }
        }


        private void UpdateTotals()
        {
            int totalItems = _orderItems.Sum(i => i.Quantity);
            decimal totalPrice = _orderItems.Sum(i => i.TotalPrice);

            TotalItemsTextBlock.Text = $"Товаров: {totalItems}";
            TotalPriceTextBlock.Text = $"Итого: {totalPrice:C}";
        }

        private void PaymentOption_Checked(object sender, RoutedEventArgs e)
        {
            if (CardPaymentPanel == null || CashOnDeliveryOptions == null)
                return;

            if (sender is RadioButton rb)
            {
                if (rb.Content.ToString() == "Онлайн")
                {
                    CardPaymentPanel.Visibility = Visibility.Visible;
                    CashOnDeliveryOptions.Visibility = Visibility.Collapsed;
                }
                else if (rb.Content.ToString() == "Оплата при получении")
                {
                    CardPaymentPanel.Visibility = Visibility.Collapsed;
                    CashOnDeliveryOptions.Visibility = Visibility.Visible;
                }
            }
        }

        private void ShowPickupOnMap_Click(object sender, RoutedEventArgs e)
        {
            string address = PickupPointsComboBox.SelectedItem as string;
            if (address == "г. Москва, Нахимовский проспект, 21")
                Process.Start(new ProcessStartInfo("https://yandex.ru/maps/-/CLumQP3i") { UseShellExecute = true });
            else if (address == "г. Москва, Нежинская улица, 7")
                Process.Start(new ProcessStartInfo("https://yandex.ru/maps/-/CLumUMnI") { UseShellExecute = true });
        }

        private void CardNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = Regex.Replace(textBox.Text, @"\D", "");
            if (text.Length > 16) text = text.Substring(0, 16);

            string formatted = "";
            for (int i = 0; i < text.Length; i++)
            {
                if (i > 0 && i % 4 == 0) formatted += " ";
                formatted += text[i];
            }

            if (textBox.Text != formatted)
            {
                textBox.Text = formatted;
                textBox.CaretIndex = formatted.Length;
            }
        }

        private void CardExpiryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = Regex.Replace(textBox.Text, @"\D", "");
            if (text.Length > 4) text = text.Substring(0, 4);

            string formatted = text;
            if (text.Length >= 2)
            {
                formatted = text.Insert(2, "/");
                if (formatted.Length > 5) formatted = formatted.Substring(0, 5);
            }

            if (textBox.Text != formatted)
            {
                textBox.Text = formatted;
                textBox.CaretIndex = formatted.Length;
            }
        }

        private void CardCvvTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string text = Regex.Replace(textBox.Text, @"\D", "");
            if (text.Length > 3) text = text.Substring(0, 3);

            if (textBox.Text != text)
            {
                textBox.Text = text;
                textBox.CaretIndex = text.Length;
            }
        }

        private async void SubmitOrder_Click(object sender, RoutedEventArgs e)
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

            string deliveryType = "Самовывоз";
            string deliveryAddress = PickupPointsComboBox.SelectedItem as string ?? "";

            if (DeliveryAddressPanel.Children.OfType<RadioButton>().FirstOrDefault(rb => rb.IsChecked == true)?.Content.ToString() == "Доставка по адресу")
            {
                deliveryType = "Доставка по адресу";
                if (string.IsNullOrWhiteSpace(AddressComboBox.Text))
                {
                    MessageBox.Show("Введите адрес доставки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                deliveryAddress = AddressComboBox.Text;
            }

            string paymentType = "";
            if (CashOnDeliveryRadio.IsChecked == true)
            {
                var selectedMethod = CashOnDeliveryOptions.Children
                    .OfType<RadioButton>()
                    .FirstOrDefault(rb => rb.IsChecked == true);

                paymentType = selectedMethod?.Content.ToString() switch
                {
                    "Наличными" => "Оплата при получении (наличные)",
                    "Картой" => "Оплата при получении (карта)",
                    _ => "Оплата при получении"
                };
            }
            else if (CardPaymentPanel.Visibility == Visibility.Visible)
            {
                paymentType = "Онлайн";

                string cardNumber = Regex.Replace(CardNumberTextBox.Text, @"\D", "");
                string expiry = CardExpiryTextBox.Text;
                string cvv = CardCvvTextBox.Text;

                if (cardNumber.Length < 16 || !Regex.IsMatch(expiry, @"^(0[1-9]|1[0-2])\/\d{2}$") || !Regex.IsMatch(cvv, @"^\d{3}$"))
                {
                    MessageBox.Show("Проверьте данные карты.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_originalPayment == null ||
                    _originalPayment.CardNumber != cardNumber ||
                    _originalPayment.Expiry != expiry ||
                    _originalPayment.Cvv != cvv)
                {
                    var paymentDto = new UserPaymentDto
                    {
                        CardNumber = cardNumber,
                        Expiry = expiry,
                        Cvv = cvv
                    };

                    try
                    {
                        await ApiHelper.PostAsync("Users/PaymentInfo", paymentDto);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Не удалось сохранить данные карты: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }

            var orderDto = new CreateOrderDto
            {
                FirstName = FirstNameTextBox.Text,
                LastName = LastNameTextBox.Text,
                DeliveryType = deliveryType,
                DeliveryAddress = deliveryAddress,
                PaymentType = paymentType
            };


            try
            {
                bool result = await ApiHelper.PostAsync<bool>("Orders", orderDto);
                if (result)
                {
                    MessageBox.Show("Заказ успешно создан! Подтверждение отправлено на вашу почту.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    FirstNameTextBox.Text = "";
                    LastNameTextBox.Text = "";
                    AddressComboBox.Text = "";
                    CardNumberTextBox.Text = "";
                    CardExpiryTextBox.Text = "";
                    CardCvvTextBox.Text = "";

                    _mainFrame.Navigate(new CatalogPage(_mainFrame));
                }
                else
                {
                    MessageBox.Show("Не удалось создать заказ. Попробуйте позже.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddressComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;

            string query = combo.Text;
            if (string.IsNullOrWhiteSpace(query)) return;

            var suggestions = await DaDataHelper.GetAddressSuggestions(query);

            combo.ItemsSource = suggestions;
        }

        private void AddressComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AddressComboBox.SelectedItem is string addr)
            {
                AddressComboBox.Text = addr;
                AddressComboBox.IsDropDownOpen = false;
            }
        }

        private void AddressComboBox_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var combo = sender as ComboBox;
            if (combo == null) return;
            combo.IsDropDownOpen = true;
            combo.Focus();
        }

        private void FirstNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatRussianName(FirstNameTextBox);
        }

        private void LastNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FormatRussianName(LastNameTextBox);
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
    }

    public class UserCheckoutDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string DeliveryType { get; set; }       // Самовывоз / Доставка по адресу
        public string DeliveryAddress { get; set; }    // Адрес доставки

        public string PaymentType { get; set; }        // Онлайн / Оплата при получении
        public string CardNumber { get; set; }
        public string Expiry { get; set; }
        public string Cvv { get; set; }
    }

    public class UserPaymentDto
    {
        public string CardNumber { get; set; }
        public string Expiry { get; set; }
        public string Cvv { get; set; }
    }

    public class UserDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class OrderItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public string MainImageUrl { get; set; }
    }

    public class CreateOrderDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DeliveryType { get; set; }
        public string DeliveryAddress { get; set; }
        public string PaymentType { get; set; }
    }
}