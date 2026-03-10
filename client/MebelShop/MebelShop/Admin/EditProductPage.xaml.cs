using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MebelShop.Helpers;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MebelShop.Admin
{
    public partial class EditProductPage : Page
    {
        private readonly Frame _mainFrame;
        private int _productId;
        private List<CategoryViewModel> _categories;
        private ObservableCollection<ProductImageEditViewModel> _images = new();
        private List<CharacteristicViewModel> _characteristics;

        public EditProductPage(Frame mainFrame, int productId)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            _productId = productId;
            LoadCategories();
            LoadProduct();
        }

        private async void LoadCategories()
        {
            try
            {
                _categories = await ApiHelper.GetAsync<List<CategoryViewModel>>("Categories/All");
                CategoryComboBox.ItemsSource = _categories;
                CategoryComboBox.DisplayMemberPath = "NameCategory";
                CategoryComboBox.SelectedValuePath = "IdCategory";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки категорий: {ex.Message}");
            }
        }

        private async void LoadProduct()
        {
            try
            {
                // Получаем данные с сервера (возвращается JsonElement)
                var productData = await ApiHelper.GetAsync<System.Text.Json.JsonElement>($"Products/{_productId}/Edit");

                // Достаём части из JSON
                var product = productData.GetProperty("product");
                var images = productData.GetProperty("images");
                var characteristics = productData.GetProperty("characteristics");

                // Заполняем основные поля
                ProductNameTextBox.Text = product.GetProperty("productName").GetString();
                DescriptionTextBox.Text = product.GetProperty("description").GetString();
                decimal price = product.GetProperty("price").GetDecimal();
                PriceTextBox.Text = price % 1 == 0
                    ? ((int)price).ToString()
                    : price.ToString("0.##");

                QuantityTextBox.Text = product.GetProperty("stockQuantity").ToString();

                int categoryId = product.GetProperty("categoryId").GetInt32();
                CategoryComboBox.SelectedValue = categoryId;

                // Загружаем характеристики для выбранной категории
                await LoadCharacteristics(categoryId);

                // Подставляем значения характеристик
                foreach (var ch in characteristics.EnumerateArray())
                {
                    int characteristicId = ch.GetProperty("characteristicId").GetInt32();
                    string value = ch.GetProperty("value").GetString();

                    var tb = CharacteristicsPanel.Children
                        .OfType<StackPanel>()
                        .SelectMany(sp => sp.Children.OfType<TextBox>())
                        .FirstOrDefault(t => (t.Tag as CharacteristicViewModel)?.IdCharacteristic == characteristicId);

                    if (tb != null)
                        tb.Text = value;
                }

                // Загружаем изображения
                _images.Clear();
                foreach (var img in images.EnumerateArray())
                {
                    _images.Add(new ProductImageEditViewModel
                    {
                        Id = img.GetProperty("id").GetInt32(),
                        Url = img.GetProperty("imageUrl").GetString(),
                        IsMain = img.GetProperty("isMain").GetBoolean()
                    });
                }

                RefreshImagesPreview();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товара: {ex.Message}");
            }
        }

        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedValue is int categoryId)
            {
                await LoadCharacteristics(categoryId);
            }
        }

        private async Task LoadCharacteristics(int categoryId)
        {
            try
            {
                _characteristics = await ApiHelper.GetAsync<List<CharacteristicViewModel>>(
                    $"Categories/{categoryId}/Characteristics"
                );

                CharacteristicsPanel.Children.Clear();

                foreach (var c in _characteristics)
                {
                    var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"{c.Name} ({c.ValueType})",
                        Width = 200,
                        VerticalAlignment = VerticalAlignment.Center
                    });

                    var tb = new TextBox
                    {
                        Width = 200,
                        Tag = c,
                    };
                    panel.Children.Add(tb);

                    CharacteristicsPanel.Children.Add(panel);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки характеристик: {ex.Message}");
            }
        }

        private void AddImage_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp",
                Multiselect = true
            };

            if (ofd.ShowDialog() == true)
            {
                foreach (var path in ofd.FileNames)
                {
                    var image = new ProductImageEditViewModel
                    {
                        Path = path,
                        IsMain = _images.Count == 0
                    };
                    _images.Add(image);
                }

                RefreshImagesPreview();
            }
        }

        private void RefreshImagesPreview()
        {
            ImagesWrapPanel.Children.Clear();

            foreach (var img in _images.Where(i => !i.MarkedForDelete).ToList())
            {
                Uri uri;
                if (!string.IsNullOrEmpty(img.Path))
                {
                    uri = new Uri(img.Path);
                }
                else if (!string.IsNullOrEmpty(img.Url))
                {
                    uri = new Uri(ApiHelper.BaseImagesUrl + img.Url);
                }
                else
                {
                    continue;
                }

                var bitmap = new BitmapImage(uri);

                var border = new Border
                {
                    BorderThickness = new Thickness(img.IsMain ? 7 : 0),
                    BorderBrush = img.IsMain ? Brushes.Orange : Brushes.Transparent,
                    Margin = new Thickness(5),
                    Width = 180,
                    Height = 180
                };

                var grid = new Grid();

                var image = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    Cursor = Cursors.Hand,
                    Tag = img,
                };

                image.MouseLeftButtonUp += (s, e) =>
                {
                    foreach (var i in _images)
                        i.IsMain = false;
                    img.IsMain = true;
                    RefreshImagesPreview();
                };

                var deleteButton = new Button
                {
                    Content = "✕",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                    Tag = img
                };

                deleteButton.Click += (s, e) =>
                {
                    img.MarkedForDelete = true;

                    RefreshImagesPreview();
                };


                grid.Children.Add(image);
                grid.Children.Add(deleteButton);

                border.Child = grid;
                ImagesWrapPanel.Children.Add(border);
            }
        }

        private async void SaveProduct_Click(object sender, RoutedEventArgs e)
        {
            string name = ProductNameTextBox.Text.Trim();
            string description = DescriptionTextBox.Text.Trim();

            if (name.Length < 2)
            {
                MessageBox.Show("Название товара должно содержать минимум 2 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (description.Length < 2)
            {
                MessageBox.Show("Описание товара должно содержать минимум 2 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CategoryComboBox.SelectedValue is not int categoryId)
            {
                MessageBox.Show("Выберите категорию товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTextBox.Text, out decimal price) || price <= 0)
            {
                MessageBox.Show("Введите корректную цену (положительное число).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity < 0)
            {
                MessageBox.Show("Введите корректное количество (неотрицательное число).", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_images.Count(i => !i.MarkedForDelete) == 0)
            {
                MessageBox.Show("Добавьте хотя бы одно изображение товара.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            foreach (var child in CharacteristicsPanel.Children.OfType<StackPanel>())
            {
                var tb = child.Children.OfType<TextBox>().FirstOrDefault();
                if (tb == null) continue;

                var characteristic = tb.Tag as CharacteristicViewModel;
                if (characteristic == null) continue;

                string value = tb.Text.Trim();

                if (!string.IsNullOrEmpty(value) && characteristic.ValueType == "range" && !decimal.TryParse(value, out _))
                {
                    MessageBox.Show($"В характеристике \"{characteristic.Name}\" должно быть указано числовое значение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            try
            {
                var updatedProduct = new
                {
                    ProductName = name,
                    Description = description,
                    Price = price,
                    StockQuantity = quantity,
                    Category_ID = categoryId
                };

                await ApiHelper.PutAsync($"Products/{_productId}", updatedProduct);

                await ApiHelper.DeleteAsync($"Products/{_productId}/Characteristics");

                foreach (var child in CharacteristicsPanel.Children.OfType<StackPanel>())
                {
                    var tb = child.Children.OfType<TextBox>().FirstOrDefault();
                    if (tb == null || string.IsNullOrWhiteSpace(tb.Text)) continue;

                    var characteristic = tb.Tag as CharacteristicViewModel;
                    if (characteristic == null) continue;

                    await ApiHelper.PostAsync("Products/AddCharacteristic", new
                    {
                        Product_ID = _productId,
                        Characteristic_ID = characteristic.IdCharacteristic,
                        Value = tb.Text.Trim()
                    });
                }

                foreach (var img in _images)
                {
                    if (img.Id.HasValue)
                    {
                        await ApiHelper.PutAsync($"Products/Images/{img.Id.Value}", new { IsMain = img.IsMain });
                    }
                    else
                    {
                        var bytes = File.ReadAllBytes(img.Path);
                        var base64 = Convert.ToBase64String(bytes);

                        await ApiHelper.PostAsync("Products/UploadImage", new
                        {
                            Product_ID = _productId,
                            ImageBase64 = base64,
                            IsMain = img.IsMain
                        });
                    }
                }

                foreach (var img in _images.Where(i => i.MarkedForDelete && i.Id.HasValue))
                {
                    try
                    {
                        await ApiHelper.DeleteAsync($"Products/Images/{img.Id.Value}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении изображения: {ex.Message}");
                    }
                }

                MessageBox.Show("Товар успешно обновлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                _mainFrame.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении товара: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_mainFrame.CanGoBack)
                _mainFrame.GoBack();
        }

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = await ApiHelper.GetAsync<ProductDeleteInfo>(
                    $"Products/{_productId}/DeleteInfo"
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

                await ApiHelper.DeleteAsync($"Products/{_productId}");
                
                _mainFrame.GoBack();
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class ProductImageEditViewModel
    {
        public int? Id { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public bool IsMain { get; set; }
        public bool MarkedForDelete { get; set; }   
    }
}