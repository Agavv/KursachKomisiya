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
using MebelShop.Customer;

namespace MebelShop.Manager
{
    /// <summary>
    /// Логика взаимодействия для OrderDetailsPage.xaml
    /// </summary>
    public partial class OrderDetailsPage : Page
    {
        private Frame _mainFrame;
        public OrderDetailsPage(Frame mainFrame, int orderId, List<OrderItemDetailViewModel> items)
        {
            InitializeComponent();
            _mainFrame = mainFrame;

            OrderNumberText.Text = $"Детали заказа № {orderId}";
            OrderItemsDataGrid.ItemsSource = items;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainFrame.CanGoBack)
                _mainFrame.GoBack();
        }

        private void OrderItemsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrderItemsDataGrid.SelectedItem is not OrderItemDetailViewModel selectedOrderItem)
                return;

            _mainFrame.Navigate(new ProductDetailPage(selectedOrderItem.ProductId, _mainFrame));
        }
    }
}
