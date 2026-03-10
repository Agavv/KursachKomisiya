using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;

namespace MebelShop.Admin
{
    public partial class ProductsPage : Page
    {
        private readonly Frame _mainFrame;
        private List<ProductAdminViewModel> _allProducts = new();

        public ProductsPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            Loaded += ProductsPage_Loaded;
        }

        private async void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadProducts();
        }

        public async Task LoadProducts()
        {
            try
            {
                _allProducts = await ApiHelper.GetAsync<List<ProductAdminViewModel>>("Products/All");
                ProductsDataGrid.ItemsSource = _allProducts;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchCategoryTextBox.Text = string.Empty;
            PriceTextBox.Text = string.Empty;
            StockTextBox.Text = string.Empty;
            ProductsDataGrid.ItemsSource = _allProducts;
        }

        private void ApplyFilters()
        {
            if (_allProducts == null) return;

            string searchProduct = SearchTextBox.Text.Trim().ToLower();
            string searchCategory = SearchCategoryTextBox.Text.Trim().ToLower();
            string priceText = PriceTextBox.Text.Trim();
            string stockText = StockTextBox.Text.Trim();

            decimal.TryParse(priceText, out decimal maxPrice);
            int.TryParse(stockText, out int maxStock);

            var filtered = _allProducts.Where(p =>
                (string.IsNullOrEmpty(searchProduct) ||
                    p.ProductName.ToLower().Contains(searchProduct) ||
                    p.IdProduct.ToString().Contains(searchProduct)) &&
                (string.IsNullOrEmpty(searchCategory) ||
                    p.CategoryName.ToLower().Contains(searchCategory) ||
                    p.CategoryId.ToString().Contains(searchCategory)) &&
                (maxPrice == 0 || p.Price <= maxPrice) &&
                (maxStock == 0 || p.StockQuantity <= maxStock)
            ).ToList();

            ProductsDataGrid.ItemsSource = filtered;
        }

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new AddProductPage(_mainFrame));
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is not ProductAdminViewModel product)
                return;

            _mainFrame.Navigate(new EditProductPage(_mainFrame, product.IdProduct));
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is not ProductAdminViewModel product)
                return;

            try
            {
                var info = await ApiHelper.GetAsync<ProductDeleteInfo>(
                    $"Products/{product.IdProduct}/DeleteInfo"
                );

                string message =
                    $"При удалении товара \"{info.ProductName}\" будут удалены:\n" +
                    $"- {info.ImagesCount} изображений\n" +
                    $"- {info.ReviewsCount} отзывов\n" +
                    $"- {info.CartItemsCount} позиций в корзинах\n" +
                    $"- {info.OrderItemsCount} товаров в заказах\n\n" +
                    "Вы действительно хотите продолжить удаление?";

                var result = MessageBox.Show(message, "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                    return;

                await ApiHelper.DeleteAsync($"Products/{product.IdProduct}");
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProductsDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is not ProductAdminViewModel product)
                return;

            _mainFrame.Navigate(new EditProductPage(_mainFrame, product.IdProduct));
        }

        private async void ImportProducts_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                Title = "Выберите файл для импорта товаров"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;

                try
                {
                    var fileBytes = File.ReadAllBytes(filePath);

                    var result = await ApiHelper.PostFileAsync("Products/import", fileBytes, Path.GetFileName(filePath));

                    var resultObj = JsonSerializer.Deserialize<JsonElement>(result);
                    if (resultObj.TryGetProperty("message", out var messageProp))
                    {
                        string message = messageProp.GetString();
                        if (message.Contains("успешно"))
                        {
                            _ = LoadProducts();
                            MessageBox.Show("Товары успешно импортированы.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show($"Ошибка при импорте товаров: {message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Файл несоответствует требованиям.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (FileNotFoundException ex)
                {
                    MessageBox.Show($"Файл не найден: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (IOException ex)
                {
                    MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Файл несоответствует требованиям.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

    }

    public class ProductDeleteInfo
    {
        public string ProductName { get; set; }
        public int ImagesCount { get; set; }
        public int ReviewsCount { get; set; }
        public int CartItemsCount { get; set; }
        public int OrderItemsCount { get; set; }
    }

    public class ProductAdminViewModel
    {
        public int IdProduct { get; set; }
        public int CategoryId { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int StockQuantity { get; set; }
    }
}
