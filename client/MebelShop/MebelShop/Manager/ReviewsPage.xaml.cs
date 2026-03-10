using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Customer;
using MebelShop.Helpers;

namespace MebelShop.Manager
{
    public partial class ReviewsPage : Page
    {
        private Frame _mainFrame;
        private List<ReviewViewModel> _reviews = new();
        private int _selectedReviewId;
        private List<ReviewViewModel> _filteredReviews = new();

        public ReviewsPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            LoadReviews();
        }

        public async void LoadReviews()
        {
            try
            {
                _reviews = await ApiHelper.GetAsync<List<ReviewViewModel>>("Reviews/All");

                foreach (var item in _reviews)
                {
                    if (!string.IsNullOrEmpty(item.Text) && item.Text.Contains("Ответ магазина:"))
                        item.ReplyButtonText = "Обновить ответ";
                    else
                        item.ReplyButtonText = "Добавить ответ";

                    if (!string.IsNullOrEmpty(item.ProductImageUrl))
                        item.ProductImageUrl = $"{ApiHelper.BaseImagesUrl}{item.ProductImageUrl}";
                }

                _filteredReviews = _reviews;
                ReviewsItemsControl.ItemsSource = _reviews;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить отзывы: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (_reviews == null) return;

            var filtered = _reviews.AsEnumerable();

            if (RatingFilterComboBox?.SelectedItem is ComboBoxItem ratingItem &&
                ratingItem.Content?.ToString() != "Все")
            {
                if (int.TryParse(ratingItem.Content.ToString(), out int rating))
                    filtered = filtered.Where(r => r.Rating == rating);
            }

            if (DateFilterComboBox?.SelectedItem is ComboBoxItem dateItem && dateItem.Content?.ToString() != "Все даты")
            {
                DateTime fromDate = dateItem.Content switch
                {
                    "За последние 7 дней" => DateTime.Now.AddDays(-7),
                    "За месяц" => DateTime.Now.AddMonths(-1),
                    "За год" => DateTime.Now.AddYears(-1),
                    _ => DateTime.MinValue
                };

                if (fromDate != DateTime.MinValue)
                    filtered = filtered.Where(r => r.CreatedAt >= fromDate);
            }

            if (ReviewsItemsControl != null)
            {
                ReviewsItemsControl.ItemsSource = filtered.ToList();
            }
        }

        private void DialogCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogHost.CloseDialogCommand.Execute(null, ReviewDialogHost);
        }

        private async void DialogSave_Click(object sender, RoutedEventArgs e)
        {
            string replyText = DialogResponseTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(replyText))
            {
                MessageBox.Show("Введите текст ответа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                await ApiHelper.PutAsync($"Reviews/reply/{_selectedReviewId}", replyText);

                DialogHost.CloseDialogCommand.Execute(null, ReviewDialogHost);

                LoadReviews();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении ответа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ReplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ReviewViewModel selectedReview)
            {
                _selectedReviewId = selectedReview.IdReview;
                DialogResponseTextBox.Text = ""; // очистка поля
                DialogHost.OpenDialogCommand.Execute(null, ReviewDialogHost);
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ReviewViewModel selectedReview)
            {
                var result = MessageBox.Show(
                    "Вы действительно хотите удалить этот отзыв?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await ApiHelper.DeleteAsync($"Reviews/{selectedReview.IdReview}");
                        LoadReviews();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при удалении отзыва: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Image_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image image && image.DataContext is ReviewViewModel review)
            {
                int productId = review.ProductId;
                _mainFrame.Navigate(new ProductDetailPage(productId, _mainFrame));
            }
        }

        private void ResetFilters_Click(object sender, RoutedEventArgs e)
        {
            if (RatingFilterComboBox.Items.Count > 0)
                RatingFilterComboBox.SelectedIndex = 0;

            if (DateFilterComboBox.Items.Count > 0)
                DateFilterComboBox.SelectedIndex = 0;

            ApplyFilters();
        }
    }

    public class ReviewViewModel
    {
        public int IdReview { get; set; }
        public int ProductId { get; set; }
        public string ProductImageUrl { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Text { get; set; }

        public string ReplyButtonText { get; set; } = "Добавить ответ";
    }
}
