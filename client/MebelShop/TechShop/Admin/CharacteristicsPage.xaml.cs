using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;

namespace MebelShop.Admin
{
    public partial class CharacteristicsPage : Page
    {
        private readonly Frame _mainFrame;
        private readonly int _categoryId;
        private bool _isEditMode = false;
        private CharacteristicAdminViewModel _selectedCharacteristic;
        private List<CharacteristicAdminViewModel> _allCharacteristics = new();

        public CharacteristicsPage(Frame mainFrame, int categoryId, string categoryName)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            _categoryId = categoryId;
            CharacteristicsTitle.Text = $"Характеристики категории: {categoryName}";
            LoadCharacteristics();
        }

        private async void LoadCharacteristics()
        {
            try
            {
                _allCharacteristics = await ApiHelper.GetAsync<List<CharacteristicAdminViewModel>>(
                    $"Categories/{_categoryId}/Characteristics"
                );
                CharacteristicsDataGrid.ItemsSource = _allCharacteristics;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки характеристик: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            _isEditMode = false;
            CharacteristicNameTextBox.Text = string.Empty;
            ValueTypeComboBox.SelectedIndex = 0;

            CharacteristicDialogHost.DataContext = new { DialogTitle = "Добавление характеристики" };
            CharacteristicDialogHost.IsOpen = true;
        }

        private void EditCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (CharacteristicsDataGrid.SelectedItem is not CharacteristicAdminViewModel characteristic)
                return;

            _isEditMode = true;
            _selectedCharacteristic = characteristic;

            CharacteristicNameTextBox.Text = characteristic.Name;
            ValueTypeComboBox.SelectedItem =
                ValueTypeComboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => (string)i.Content == characteristic.ValueType);

            CharacteristicDialogHost.DataContext = new { DialogTitle = "Редактирование характеристики" };
            CharacteristicDialogHost.IsOpen = true;
        }

        private async void DeleteCharacteristic_Click(object sender, RoutedEventArgs e)
        {
            if (CharacteristicsDataGrid.SelectedItem is not CharacteristicAdminViewModel characteristic)
                return;

            try
            {
                // Получаем информацию об удалении
                var info = await ApiHelper.GetAsync<CharacteristicDeleteInfo>(
                    $"Categories/Characteristics/{characteristic.IdCharacteristic}/DeleteInfo"
                );

                string message =
                    $"Характеристика \"{info.CharacteristicName}\" используется у {info.ProductsUsing} товаров.\n" +
                    "Если вы удалите её, эти связи будут также удалены.\n\n" +
                    "Вы действительно хотите удалить характеристику?";

                var result = MessageBox.Show(message, "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                // Удаление характеристики
                await ApiHelper.DeleteAsync($"Categories/Characteristics/{characteristic.IdCharacteristic}");
                LoadCharacteristics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DialogSave_Click(object sender, RoutedEventArgs e)
        {
            var name = CharacteristicNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название характеристики.");
                return;
            }

            var type = (ValueTypeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrEmpty(type))
            {
                MessageBox.Show("Выберите тип значения.");
                return;
            }

            try
            {
                if (_isEditMode)
                {
                    await ApiHelper.PutAsync($"Categories/Characteristics/{_selectedCharacteristic.IdCharacteristic}",
                        new { Name = name, ValueType = type });
                }
                else
                {
                    await ApiHelper.PostAsync($"Categories/{_categoryId}/Characteristics",
                        new { Name = name, ValueType = type });
                }

                CharacteristicDialogHost.IsOpen = false;
                LoadCharacteristics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainFrame.CanGoBack)
            {
                _mainFrame.GoBack();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(query))
            {
                CharacteristicsDataGrid.ItemsSource = _allCharacteristics;
                return;
            }

            var filtered = _allCharacteristics.Where(c =>
                (c.Name?.ToLower().Contains(query) ?? false) ||
                (c.ValueType?.ToLower().Contains(query) ?? false)
            ).ToList();

            CharacteristicsDataGrid.ItemsSource = filtered;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CharacteristicsDataGrid.ItemsSource = _allCharacteristics;
        }
    }

    public class CharacteristicDeleteInfo
    {
        public string CharacteristicName { get; set; }
        public string CategoryName { get; set; }
        public int ProductsUsing { get; set; }
    }

    public class CharacteristicAdminViewModel
    {
        public int IdCharacteristic { get; set; }
        public string Name { get; set; }
        public string ValueType { get; set; }
    }
}
