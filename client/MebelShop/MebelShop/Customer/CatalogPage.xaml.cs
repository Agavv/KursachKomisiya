using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MebelShop.Helpers;

namespace MebelShop.Customer
{
    public partial class CatalogPage : Page
    {
        private Frame _mainFrame;
        private Dictionary<int, List<CharacteristicDto>> _categoryCharacteristics = new();
        public int CurrentPage = 1;
        public int PageSize = 20; //в качестве тест пока так, пока не появится больше товаров в каталоге
        public int TotalCount = 0;
        private List<ProductViewModel> AllProducts = new();

        public CatalogPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadProducts();
            LoadCategories();
        }

        private bool _isFilterOpen = false;

        private void ToggleFilterButton_Click(object sender, RoutedEventArgs e)
        {
            FilterColumn.Width = _isFilterOpen ? new GridLength(0) : new GridLength(250);
            _isFilterOpen = !_isFilterOpen;
        }

        public async void LoadProducts(bool append = false)
        {
            string search = SearchTextBox.Text.Trim();
            int? categoryId = CategoryComboBox.SelectedValue as int?;
            string sortBy = null;
            if (SortComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                sortBy = selectedItem.Content.ToString() switch
                {
                    "Сначала дешёвые" => "price_asc",
                    "Сначала дорогие" => "price_desc",
                    "Сначала с высоким рейтингом" => "rating_desc",
                    "Сначала популярные" => "reviews_desc",
                    _ => null
                };
            }

            var listFilters = new Dictionary<int, string>();
            var minRanges = new Dictionary<int, string>();
            var maxRanges = new Dictionary<int, string>();

            if (categoryId.HasValue && _categoryCharacteristics.ContainsKey(categoryId.Value))
            {
                foreach (var c in _categoryCharacteristics[categoryId.Value])
                {
                    if (c.ValueType == "list")
                    {
                        var cb = CharacteristicsPanel.Children.OfType<ComboBox>().FirstOrDefault(cb => (int)cb.Tag == c.Id);
                        if (cb != null && cb.SelectedItem != null)
                        {
                            listFilters[c.Id] = cb.SelectedItem.ToString();
                        }
                    }
                    else if (c.ValueType == "range")
                    {
                        var rangePanel = CharacteristicsPanel.Children.OfType<StackPanel>().FirstOrDefault(sp => sp.Children.OfType<TextBox>().Any(tb => (int)tb.Tag == c.Id));
                        if (rangePanel != null)
                        {
                            var fromBox = rangePanel.Children[0] as TextBox;
                            var toBox = rangePanel.Children[1] as TextBox;

                            if (!string.IsNullOrEmpty(fromBox.Text))
                                minRanges[c.Id] = fromBox.Text;
                            if (!string.IsNullOrEmpty(toBox.Text))
                                maxRanges[c.Id] = toBox.Text;
                        }
                    }
                }
            }

            var paramsList = new List<string>();
            if (!string.IsNullOrEmpty(search))
                paramsList.Add($"SearchQuery={Uri.EscapeDataString(search)}");
            if (categoryId.HasValue)
                paramsList.Add($"CategoryId={categoryId.Value}");
            if (!string.IsNullOrEmpty(sortBy))
                paramsList.Add($"SortBy={sortBy}");
            foreach (var kv in listFilters)
                paramsList.Add($"ListFilters[{kv.Key}]={Uri.EscapeDataString(kv.Value)}");
            foreach (var kv in minRanges)
                paramsList.Add($"MinRanges[{kv.Key}]={kv.Value}");
            foreach (var kv in maxRanges)
                paramsList.Add($"MaxRanges[{kv.Key}]={kv.Value}");

            paramsList.Add($"page={CurrentPage}");
            paramsList.Add($"pageSize={PageSize}");

            var queryString = string.Join("&", paramsList);
            var url = $"Products/catalog" + (string.IsNullOrEmpty(queryString) ? "" : "?" + queryString);

            try
            {
                var response = await ApiHelper.GetAsync<CatalogResponse>(url);

                foreach (var p in response.Products)
                {
                    if (!string.IsNullOrEmpty(p.MainImageUrl))
                        p.MainImageUrl = $"{ApiHelper.BaseImagesUrl}{p.MainImageUrl}";
                }

                TotalCount = response.TotalCount;

                if (append)
                {
                    AllProducts.AddRange(response.Products);
                }
                else
                {
                    AllProducts = response.Products;
                }

                ProductsItemsControl.ItemsSource = null;
                ProductsItemsControl.ItemsSource = AllProducts;

                LoadMoreButton.Visibility = AllProducts.Count >= TotalCount ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки товаров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadCategories()
        {
            try
            {
                var categories = await ApiHelper.GetAsync<List<CategoryDto>>("Products/categories");
                CategoryComboBox.ItemsSource = categories;
                CategoryComboBox.DisplayMemberPath = "NameCategory";
                CategoryComboBox.SelectedValuePath = "IdCategory";
            }
            catch
            {

            }
        }

        private async void CategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CharacteristicsPanel.Children.Clear();
            if (CategoryComboBox.SelectedValue == null) return;

            int categoryId = (int)CategoryComboBox.SelectedValue;

            if (!_categoryCharacteristics.ContainsKey(categoryId))
            {
                try
                {
                    var characteristics = await ApiHelper.GetAsync<List<CharacteristicDto>>($"Products/categories/{categoryId}/characteristics");
                    _categoryCharacteristics[categoryId] = characteristics;
                }
                catch
                {
                    MessageBox.Show("Не удалось загрузить характеристики");
                    return;
                }
            }

            foreach (var c in _categoryCharacteristics[categoryId])
            {
                TextBlock title = new() { Text = c.Name, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 0, 2) };
                CharacteristicsPanel.Children.Add(title);

                if (c.ValueType == "list")
                {
                    ComboBox cb = new() { Margin = new Thickness(0, 0, 0, 5), Tag = c.Id };
                    cb.ItemsSource = c.Values;
                    CharacteristicsPanel.Children.Add(cb);
                }
                else if (c.ValueType == "range")
                {
                    StackPanel rangePanel = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 5) };

                    TextBox fromBox = new TextBox() { Width = 60, Margin = new Thickness(0, 0, 5, 0), Tag = c.Id };
                    HintAssist.SetHint(fromBox, $"От ({c.MinValue})");
                    fromBox.TextChanged += NumericTextBox_TextChanged;

                    TextBox toBox = new TextBox() { Width = 60, Tag = c.Id };
                    HintAssist.SetHint(toBox, $"До ({c.MaxValue})");
                    toBox.TextChanged += NumericTextBox_TextChanged;

                    rangePanel.Children.Add(fromBox);
                    rangePanel.Children.Add(toBox);
                    CharacteristicsPanel.Children.Add(rangePanel);
                }
            }
        }

        private void NumericTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            string digitsOnly = Regex.Replace(textBox.Text, @"\D", "");

            if (textBox.Text != digitsOnly)
            {
                int caretIndex = textBox.CaretIndex;
                textBox.Text = digitsOnly;
                textBox.CaretIndex = Math.Min(caretIndex, digitsOnly.Length);
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadProducts();
        }

        private void SortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CurrentPage = 1;
            LoadProducts();
        }

        private void Card_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is ProductViewModel product)
            {
                _mainFrame.Navigate(new ProductDetailPage(product.Id, _mainFrame));
            }
        }

        private void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Clear();
            SortComboBox.SelectedIndex = 0;
            CategoryComboBox.SelectedIndex = -1;
            CharacteristicsPanel.Children.Clear();

            CurrentPage = 1;

            LoadProducts();
        }


        private async void CartButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ProductViewModel product)
            {
                if (product.IsInCart)
                {
                    _mainFrame.Navigate(new CartPage(_mainFrame));
                }
                else
                {
                    try
                    {
                        await ApiHelper.PostAsync($"Cart/{product.Id}", null);
                        product.IsInCart = true;

                        button.Content = product.CartButtonText;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка добавления в корзину: {ex.Message}");
                    }
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox) return;

            if (string.IsNullOrEmpty(textBox.Text))
            {
                CurrentPage = 1;
                LoadProducts();
            }
        }

        private void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (AllProducts.Count < TotalCount)
            {
                CurrentPage++;
                LoadProducts(append: true);
            }
        }
    }

    public class CatalogResponse
    {
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<ProductViewModel> Products { get; set; }
    }

    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class ProductViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public string MainImageUrl { get; set; }
        public double AverageRating { get; set; }
        public int ReviewsCount { get; set; }
        public bool IsInCart { get; set; }
        public string CartButtonText => IsInCart ? "В корзине" : "Купить";
    }

    public class CategoryDto
    {
        public int IdCategory { get; set; }
        public string NameCategory { get; set; }
    }

    public class CharacteristicDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ValueType { get; set; }
        public List<string> Values { get; set; } = new();
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }
}