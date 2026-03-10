using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class OrderDetailsPage : Page
    {
        private Frame mainFrame;
        private readonly int orderId;
        private List<OrderItemViewModel> orderItems;
        public OrderDetailsPage(Frame _mainFrame, int _orderId)
        {
            InitializeComponent();
            this.mainFrame = _mainFrame;
            orderId = _orderId;

            Loaded += (s, e) => LoadOrderDetails();
        }

        public async void LoadOrderDetails()
        {
            try
            {
                var order = await ApiHelper.GetAsync<OrderDetailsDto>($"Orders/Details/{orderId}");
                if (order == null) return;

                orderItems = order.Items
                    .Select(i => new OrderItemViewModel
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        TotalPrice = i.TotalPrice,
                        MainImageUrl = $"{ApiHelper.BaseImagesUrl}{i.MainImageUrl}"
                    })
                    .ToList();

                OrderItemsControl.ItemsSource = orderItems;

                TotalPriceTextBlock.Text = $"Итого: {order.TotalPrice:N2} ₽";

                OrderHeaderTextBlock.Text = $"Детали заказа №{orderId}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке заказа: {ex.Message}");
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            mainFrame.GoBack();
        }

        private async void RepeatOrder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = await ApiHelper.PostAsync<object>($"Orders/Repeat/{orderId}", null);

                if (result != null)
                {

                    mainFrame.Navigate(new CartPage(mainFrame));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при повторении заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is OrderItemViewModel item)
            {
                int productId = item.ProductId;
                 mainFrame.Navigate(new ProductDetailPage(productId, mainFrame));
            }
        }
    }

    public class OrderDetailsDto
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }
        public decimal TotalPrice { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = new List<OrderItemViewModel>();
    }
}
