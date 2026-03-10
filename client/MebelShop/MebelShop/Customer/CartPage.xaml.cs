using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class CartPage : Page
    {
        private Frame _mainFrame;
        private List<CartItemViewModel> _cartItems = new();

        public CartPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadCart();
        }

        public async void LoadCart()
        {
            try
            {
                _cartItems = await ApiHelper.GetAsync<List<CartItemViewModel>>("Cart");

                foreach (var item in _cartItems)
                {
                    if (!string.IsNullOrEmpty(item.ImageUrl))
                        item.ImageUrl = $"{ApiHelper.BaseImagesUrl}{item.ImageUrl}";
                }

                CartItemsControl.ItemsSource = _cartItems;
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки корзины: {ex.Message}");
            }
        }

        private void UpdateTotals()
        {
            int totalCount = _cartItems.Sum(i => i.Quantity);
            decimal totalPrice = _cartItems.Sum(i => i.Price * i.Quantity);

            TotalCountText.Text = $"Всего товаров: {totalCount}";
            TotalPriceText.Text = $"Итого: {totalPrice} ₽";
        }

        private async void IncreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                try
                {
                    await ApiHelper.PostAsync<object>($"Cart/{productId}/increase", null);
                    LoadCart();
                }
                catch (HttpRequestException ex)
                {
                    
                }
            }
        }


        private async void DecreaseQuantity_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                await ApiHelper.PostAsync<object>($"Cart/{productId}/decrease", null);
                LoadCart();
            }
        }

        private async void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int productId)
            {
                await ApiHelper.DeleteAsync<object>($"Cart/{productId}");
                LoadCart();
            }
        }

        private void Checkout_Click(object sender, RoutedEventArgs e)
        {
            if (_cartItems == null || !_cartItems.Any())
            {
                MessageBox.Show("Корзина пуста. Добавьте товары, чтобы оформить заказ.");
                return;
            }

            _mainFrame.Navigate(new CheckoutPage(_mainFrame));
        }

        private void Product_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is CartItemViewModel item)
            {
                _mainFrame.Navigate(new ProductDetailPage(item.ProductId, _mainFrame));
            }
        }
    }

    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string ImageUrl { get; set; }
    }
}
