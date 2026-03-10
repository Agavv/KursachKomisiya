using MaterialDesignThemes.Wpf;
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

namespace MebelShop.Manager
{
    public partial class OrdersPage : Page
    {
        private Frame _mainFrame;
        private List<OrderViewModel> _allOrders = new();
        public OrdersPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadOrders();
        }

        public async void LoadOrders()
        {
            try
            {
                _allOrders = await ApiHelper.GetAsync<List<OrderViewModel>>("Orders/All");
                OrdersDataGrid.ItemsSource = _allOrders;

                OrderIdTextBox.TextChanged += Filter_Changed;
                UserIdTextBox.TextChanged += Filter_Changed;
                FirstNameTextBox.TextChanged += Filter_Changed;
                LastNameTextBox.TextChanged += Filter_Changed;
                DateFromPicker.SelectedDateChanged += Filter_Changed;
                DateToPicker.SelectedDateChanged += Filter_Changed;
                StatusComboBox.SelectionChanged += Filter_Changed;
                DeliveryTypeComboBox.SelectionChanged += Filter_Changed;
                PaymentTypeComboBox.SelectionChanged += Filter_Changed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки заказов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Filter_Changed(object sender, EventArgs e)
        {
            if (_allOrders == null) return;
            var filtered = _allOrders.AsEnumerable();

            // ID заказа (от 1 символа)
            if (!string.IsNullOrWhiteSpace(OrderIdTextBox.Text) && int.TryParse(OrderIdTextBox.Text, out int orderId))
                filtered = filtered.Where(o => o.IdOrder.ToString().StartsWith(OrderIdTextBox.Text));

            // ID пользователя (от 1 символа)
            if (!string.IsNullOrWhiteSpace(UserIdTextBox.Text) && int.TryParse(UserIdTextBox.Text, out int userId))
                filtered = filtered.Where(o => o.UserId.ToString().StartsWith(UserIdTextBox.Text));

            // Имя (от 2 символов)
            if (!string.IsNullOrWhiteSpace(FirstNameTextBox.Text) && FirstNameTextBox.Text.Length >= 2)
                filtered = filtered.Where(o => o.FirstName.Contains(FirstNameTextBox.Text, StringComparison.OrdinalIgnoreCase));

            // Фамилия (от 2 символов)
            if (!string.IsNullOrWhiteSpace(LastNameTextBox.Text) && LastNameTextBox.Text.Length >= 2)
                filtered = filtered.Where(o => o.LastName.Contains(LastNameTextBox.Text, StringComparison.OrdinalIgnoreCase));

            // Дата от
            if (DateFromPicker.SelectedDate.HasValue)
                filtered = filtered.Where(o => o.CreatedAt.Date >= DateFromPicker.SelectedDate.Value.Date);

            // Дата до
            if (DateToPicker.SelectedDate.HasValue)
                filtered = filtered.Where(o => o.CreatedAt.Date <= DateToPicker.SelectedDate.Value.Date);

            // Статус
            if (StatusComboBox.SelectedItem is ComboBoxItem cbItem && cbItem.Content.ToString() != "Все (статус)")
                filtered = filtered.Where(o => o.Status == cbItem.Content.ToString());

            // Фильтр по способу доставки
            if (DeliveryTypeComboBox.SelectedItem is ComboBoxItem deliveryItem && deliveryItem.Content.ToString() != "Все (доставка)")
                filtered = filtered.Where(o => o.DeliveryType == deliveryItem.Content.ToString());

            // Фильтр по способу оплаты
            if (PaymentTypeComboBox.SelectedItem is ComboBoxItem paymentItem && paymentItem.Content.ToString() != "Все (оплата)")
                filtered = filtered.Where(o => o.PaymentType == paymentItem.Content.ToString());

            OrdersDataGrid.ItemsSource = filtered.ToList();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            OrderIdTextBox.Text = "";
            UserIdTextBox.Text = "";
            FirstNameTextBox.Text = "";
            LastNameTextBox.Text = "";
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;
            StatusComboBox.SelectedIndex = 0;
            DeliveryTypeComboBox.SelectedIndex = 0;
            PaymentTypeComboBox.SelectedIndex = 0;

            OrdersDataGrid.ItemsSource = _allOrders;
        }

        private async void OrdersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is not OrderViewModel selectedOrder)
                return;

            try
            {
                // Получаем детали заказа с сервера
                var orderItems = await ApiHelper.GetAsync<List<OrderItemDetailViewModel>>($"Orders/{selectedOrder.IdOrder}/Items");

                _mainFrame.Navigate(new OrderDetailsPage(_mainFrame, selectedOrder.IdOrder, orderItems));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveStatus_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is not OrderViewModel order)
                return;

            if (DialogStatusComboBox.SelectedItem is not ComboBoxItem statusItem)
            {
                MessageBox.Show("Выберите статус.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string newStatus = statusItem.Content.ToString();

            if (newStatus == order.Status)
            {
                MessageBox.Show("Статус заказа уже установлен в это значение.",
                                "Информация",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            string comment = DialogCommentTextBox.Text?.Trim();

            try
            {
                var dto = new UpdateOrderStatusDto
                {
                    OrderId = order.IdOrder,
                    NewStatus = newStatus,
                    Comment = comment
                };

                // API вызов
                bool result = await ApiHelper.PostAsync<bool>("Orders/UpdateStatus", dto);

                if (result)
                {
                    order.Status = newStatus; // обновим локально
                    OrdersDataGrid.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("Не удалось обновить статус заказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                DialogHost.CloseDialogCommand.Execute(null, OrdersDialogHost);
            }
        }

        private void ChangeStatus_Click(object sender, RoutedEventArgs e)
        {
            if (OrdersDataGrid.SelectedItem is not OrderViewModel order)
                return;

            // Если заказ уже завершён или отменён – запрещаем изменение
            if (order.Status == "Отменен" || order.Status == "Получен")
            {
                MessageBox.Show("Изменение статуса недоступно для завершённых или отменённых заказов.",
                                "Ограничение",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }
                
            // Установим текущее значение в комбобокс
            DialogStatusComboBox.SelectedItem =
                DialogStatusComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(x => x.Content.ToString() == order.Status);

            // Очистим комментарий
            DialogCommentTextBox.Text = "";

            // Открыть диалог
            DialogHost.OpenDialogCommand.Execute(null, null);
        }
    }

    public class UpdateOrderStatusDto
    {
        public int OrderId { get; set; }
        public string NewStatus { get; set; }
        public string Comment { get; set; }
    }

    public class OrderItemDetailViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
    }

    public class OrderViewModel
    {
        public int IdOrder { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; }

        // Новые поля
        public string DeliveryType { get; set; }
        public string DeliveryAddress { get; set; }
        public string PaymentType { get; set; }
    }
}
