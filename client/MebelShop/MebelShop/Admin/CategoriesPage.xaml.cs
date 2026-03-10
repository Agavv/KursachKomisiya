using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;
using MebelShop.Manager;

namespace MebelShop.Admin
{
    public partial class CategoriesPage : Page
    {
        private Frame _mainFrame;
        private bool _isEditMode = false;
        private CategoryAdminViewModel _selectedCategory;
        private List<CategoryAdminViewModel> _allCategories = new();

        public CategoriesPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadCategories();
        }

        public async void LoadCategories()
        {
            try
            {
                _allCategories = await ApiHelper.GetAsync<List<CategoryAdminViewModel>>("Categories/All");
                CategoriesDataGrid.ItemsSource = _allCategories;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem is not CategoryAdminViewModel category)
                return;

            _selectedCategory = category;

            _isEditMode = false;
            CategoryNameTextBox.Text = string.Empty;
            CategoryDialogHost.DataContext = new { DialogTitle = "Добавление категории" };
            CategoryDialogHost.IsOpen = true;
        }

        private void EditCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem is not CategoryAdminViewModel category)
                return;

            _selectedCategory = category;

            _isEditMode = true;
            CategoryNameTextBox.Text = category.NameCategory;
            CategoryDialogHost.DataContext = new { DialogTitle = "Редактирование категории" };
            CategoryDialogHost.IsOpen = true;
        }

        private async void DeleteCategory_Click(object sender, RoutedEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem is not CategoryAdminViewModel category)
                return;

            _selectedCategory = category;

            try
            {
                var info = await ApiHelper.GetAsync<CategoryDeleteInfo>(
                    $"Categories/{_selectedCategory.IdCategory}/DeleteInfo"
                );

                string message =
                    $"При удалении категории \"{info.CategoryName}\" будут удалены:\n" +
                    $"- {info.Products} товаров\n" +
                    $"- {info.Characteristics} характеристик\n" +
                    $"- {info.Reviews} отзывов\n" +
                    $"- {info.CartItems} позиций в корзинах\n" +
                    $"- {info.OrderItems} товаров в заказах\n" +
                    $"- {info.ProductImages} изображений\n" +
                    $"- {info.ProductCharacteristics} характеристик товаров\n\n" +
                    "Продолжить удаление?";

                var result = MessageBox.Show(message, "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    await ApiHelper.DeleteAsync($"Categories/{_selectedCategory.IdCategory}");
                    LoadCategories();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DialogSave_Click(object sender, RoutedEventArgs e)
        {
            var name = CategoryNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название категории.");
                return;
            }

            try
            {
                if (_isEditMode)
                {
                    await ApiHelper.PutAsync($"Categories/{_selectedCategory.IdCategory}", new { NameCategory = name });
                }
                else
                {
                    await ApiHelper.PostAsync("Categories", new { NameCategory = name });
                }

                CategoryDialogHost.IsOpen = false;
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string query = SearchTextBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(query))
            {
                CategoriesDataGrid.ItemsSource = _allCategories;
                return;
            }

            var filtered = _allCategories.Where(c =>
                c.IdCategory.ToString().Contains(query) ||
                c.NameCategory.ToLower().Contains(query)
            ).ToList();

            CategoriesDataGrid.ItemsSource = filtered;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            CategoriesDataGrid.ItemsSource = _allCategories;
        }

        private async void CategoriesDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CategoriesDataGrid.SelectedItem is not CategoryAdminViewModel category)
                return;

            try
            {

                _mainFrame.Navigate(new CharacteristicsPage(_mainFrame, category.IdCategory, category.NameCategory));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки деталей заказа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class CategoryDeleteInfo
    {
        public string CategoryName { get; set; }
        public int Products { get; set; }
        public int Characteristics { get; set; }
        public int ProductImages { get; set; }
        public int Reviews { get; set; }
        public int CartItems { get; set; }
        public int OrderItems { get; set; }
        public int ProductCharacteristics { get; set; }
    }

    public class CategoryAdminViewModel
    {
        public int IdCategory { get; set; }
        public string NameCategory { get; set; }
        public int CharacteristicsCount { get; set; }
        public int ProductsCount { get; set; }
    }
}
