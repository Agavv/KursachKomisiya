using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;

namespace MebelShop.Admin
{
    public partial class DatabasePage : Page
    {
        private ObservableCollection<BackupViewModel> _backups = new();

        public DatabasePage()
        {
            InitializeComponent();
            BackupGrid.ItemsSource = _backups;
            Loaded += DatabasePage_Loaded;
        }

        private async void DatabasePage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadBackups();
        }

        public async Task LoadBackups()
        {
            try
            {
                var backups = await ApiHelper.GetAsync<List<BackupViewModel>>("Database/list");
                _backups.Clear();
                foreach (var b in backups)
                    _backups.Add(b);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки резервных копий: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void CreateBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = await ApiHelper.PostAsync("Database/create", new { });

                await LoadBackups();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании резервной копии:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            if (BackupGrid.SelectedItem is not BackupViewModel selected)
                return;

            if (MessageBox.Show($"Удалить резервную копию '{selected.FileName}'?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                await ApiHelper.DeleteAsync($"Database/delete/{selected.FileName}");
                await LoadBackups();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении резервной копии:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RestoreBackup_Click(object sender, RoutedEventArgs e)
        {
            if (BackupGrid.SelectedItem is BackupViewModel selected)
            {
                if (MessageBox.Show(
                    $"Вы уверены, что хотите восстановить базу данных из копии:\n{selected.FileName}?",
                    "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        var response = await ApiHelper.PostAsync("Database/restore", new { fileName = selected.FileName });
                        MessageBox.Show("База данных успешно восстановлена из резервной копии", "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (HttpRequestException ex)
                    {
                        MessageBox.Show($"Ошибка при восстановлении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите резервную копию для восстановления.");
            }
        }
    }

    public class BackupViewModel
    {
        public string FileName { get; set; }
        public DateTime CreatedAt { get; set; }
        public double SizeMB { get; set; }
    }
}
