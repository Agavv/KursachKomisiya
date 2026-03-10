        using System.Globalization;
        using System.Net.Http;
        using System.Text.Json;
        using System.Windows;
        using System.Windows.Controls;
        using System.Windows.Data;
        using System.Windows.Media;
        using MebelShop.Auth;
        using MebelShop.Helpers;

        namespace MebelShop.Customer
        {
            public partial class ProductDetailPage : Page
            {
                private int _productId;
                Frame _mainFrame;
                private List<ReviewDto> _reviews = new();

                public ProductDetailPage(int productId, Frame mainFrame)
                {
                    InitializeComponent();
                    _productId = productId;
                    LoadProductDetail();
                    _mainFrame = mainFrame;
                }

                public async void LoadProductDetail()
                {
                    try
                    {
                        var product = await ApiHelper.GetAsync<ProductDetailViewModel>($"Products/{_productId}");

                        product.ImageUrls = product.ImageUrls
                            .Select(u => $"{ApiHelper.BaseImagesUrl}{u}")
                            .ToList();

                        ProductNameText.Text = product.ProductName;
                        PriceText.Text = $"{product.Price:C}";
                        DescriptionText.Text = product.Description;

                        ImagesListBox.ItemsSource = product.ImageUrls;
                        CharacteristicsItemsControl.ItemsSource = product.Characteristics;

                        _reviews = product.Reviews
                            .OrderByDescending(r => r.IsMine)
                            .ToList();
                        
                        ReviewsItemsControl.ItemsSource = product.Reviews;

                        if (product.Reviews.Any())
                        {
                            AverageRatingText.Text = $"⭐ {product.Reviews.Average(r => r.Rating):0.0}";
                            ReviewSortPanel.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            AverageRatingText.Text = "⭐ -";
                            ReviewSortPanel.Visibility = Visibility.Collapsed;
                        }

                        ReviewsCountText.Text = $"Отзывов: {product.Reviews.Count}";

                        var json = await ApiHelper.GetAsync<JsonElement>($"Products/{_productId}/can-review");
                        bool canReview = json.GetProperty("canReview").GetBoolean();


                        AddReviewPanel.Visibility = canReview ? Visibility.Visible : Visibility.Collapsed;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
                    }
                }

                private void ApplyReviewSorting()
                {
                    if (_reviews == null) return;

                    List<ReviewDto> sorted = _reviews;

                    if (ReviewSortComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        switch (selectedItem.Content.ToString())
                        {
                            case "По умолчанию":
                                sorted = _reviews;
                                break;
                            case "Высокий рейтинг":
                                sorted = _reviews.OrderByDescending(r => r.Rating).ToList();
                                break;
                            case "Низкий рейтинг":
                                sorted = _reviews.OrderBy(r => r.Rating).ToList();
                                break;
                            case "Самые новые":
                                sorted = _reviews.OrderByDescending(r => r.CreatedAt).ToList();
                                break;
                            case "Самые старые":
                                sorted = _reviews.OrderBy(r => r.CreatedAt).ToList();
                                break;
                        }
                    }

                    ReviewsItemsControl.ItemsSource = sorted;
                }

                private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
                {
                    if (sender is Image img && img.Source != null)
                    {
                        Window imageWindow = new Window
                        {
                            Title = "Изображение",
                            Width = 800,
                            Height = 800,
                            Content = new Image
                            {
                                Source = img.Source,
                                Stretch = System.Windows.Media.Stretch.Uniform,
                                SnapsToDevicePixels = true
                            },
                            WindowStartupLocation = WindowStartupLocation.CenterOwner,
                            Owner = Window.GetWindow(this)
                        };

                        imageWindow.ShowDialog();
                    }
                }

                private void BackButton_Click(object sender, RoutedEventArgs e)
                {
                    _mainFrame.GoBack();
                }

                private async void SubmitReview_Click(object sender, RoutedEventArgs e)
                {
                    string comment = ReviewTextBox.Text.Trim();
                    int rating = (int)RatingSlider.Value;

                    if (string.IsNullOrWhiteSpace(comment))
                    {
                        MessageBox.Show("Комментарий не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    try
                    {
                        var reviewDto = new AddReviewDto
                        {
                            Comment = comment,
                            Rating = rating
                        };

                        try
                        {
                            string responseContent = await ApiHelper.PostAsync($"Products/{_productId}/reviews", reviewDto);
                        }
                        catch (HttpRequestException httpEx) when (httpEx.Message.Contains("409"))
                        {
                            var result = MessageBox.Show(
                                "Вы уже оставляли отзыв на этот товар. Хотите заменить его?",
                                "Отзыв уже существует",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Question);

                            if (result == MessageBoxResult.Yes)
                            {
                                string putResponse = await ApiHelper.PutAsync($"Products/{_productId}/reviews", reviewDto);
                            }
                        }

                        LoadProductDetail();

                        ReviewTextBox.Text = "";
                        RatingSlider.Value = 5;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        MessageBox.Show($"Ошибка запроса к серверу: {httpEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении отзыва: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }


                private void ReviewSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
                {
                    if (_reviews.Any())
                    {
                        ApplyReviewSorting();
                    }
                }

                private async void DeleteReview_Click(object sender, RoutedEventArgs e)
                {
                    var confirm = MessageBox.Show("Вы уверены, что хотите удалить отзыв?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (confirm != MessageBoxResult.Yes) return;

                    try
                    {
                        await ApiHelper.DeleteAsync($"Products/{_productId}/reviews");
                        LoadProductDetail();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }

            public class AddReviewDto
            {
                public string Comment { get; set; }
                public int Rating { get; set; }
            }

            public class ReviewDto
            {
                public string UserName { get; set; }
                public string Comment { get; set; }
                public int Rating { get; set; }
                public DateTime CreatedAt { get; set; }

                public bool IsMine { get; set; }
            }

            public class ProductDetailViewModel
            {
                public int Id { get; set; }
                public string ProductName { get; set; }
                public decimal Price { get; set; }
                public string Description { get; set; }
                public List<string> ImageUrls { get; set; } = new();
                public List<CharacteristicDetailDto> Characteristics { get; set; } = new();
                public List<ReviewDto> Reviews { get; set; } = new();
            }

        public class CharacteristicDetailDto
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
