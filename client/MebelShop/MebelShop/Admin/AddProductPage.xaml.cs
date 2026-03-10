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

namespace MebelShop.Admin
{
    public partial class AddProductPage : Page
    {
        private readonly Frame _mainFrame;
        private List<CategoryViewModel> _categories;
        private ObservableCollection<ProductImageViewModel> _images = new();
        private List<CharacteristicViewModel> _characteristics;
        public AddProductPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadCategories();
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

        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryComboBox.SelectedValue is not int categoryId) return;

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
                    var image = new ProductImageViewModel
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

            foreach (var img in _images.ToList())
            {
                var bitmap = new BitmapImage(new Uri(img.Path));

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
                    _images.Remove(img);
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

            if (_images.Count == 0)
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
                var newProduct = new
                {
                    ProductName = name,
                    Description = description,
                    Price = price,
                    StockQuantity = quantity,
                    Category_ID = categoryId
                };

                int productId = await ApiHelper.PostAsync<int>("Products", newProduct);

                foreach (var img in _images)
                {
                    var bytes = File.ReadAllBytes(img.Path);
                    var base64 = Convert.ToBase64String(bytes);

                    await ApiHelper.PostAsync("Products/UploadImage", new
                    {
                        Product_ID = productId,
                        ImageBase64 = base64,
                        IsMain = img.IsMain
                    });
                }

                foreach (var child in CharacteristicsPanel.Children.OfType<StackPanel>())
                {
                    var tb = child.Children.OfType<TextBox>().FirstOrDefault();
                    if (tb == null || string.IsNullOrWhiteSpace(tb.Text)) continue;

                    var characteristic = tb.Tag as CharacteristicViewModel;
                    if (characteristic == null) continue;

                    await ApiHelper.PostAsync("Products/AddCharacteristic", new
                    {
                        Product_ID = productId,
                        Characteristic_ID = characteristic.IdCharacteristic,
                        Value = tb.Text.Trim()
                    });
                }

                MessageBox.Show("Товар успешно добавлен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
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
    }

    public class CategoryViewModel
    {
        public int IdCategory { get; set; }
        public string NameCategory { get; set; }
    }

    public class CharacteristicViewModel
    {
        public int IdCharacteristic { get; set; }
        public string Name { get; set; }
        public string ValueType { get; set; }
    }

    public class ProductImageViewModel
    {
        public string Path { get; set; }
        public bool IsMain { get; set; }
    }
}
