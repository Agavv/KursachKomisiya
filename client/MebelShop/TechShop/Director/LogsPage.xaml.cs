using Microsoft.Win32;
using Org.BouncyCastle.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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

namespace MebelShop.Director
{
    /// <summary>
    /// Логика взаимодействия для LogsPage.xaml
    /// </summary>
    public partial class LogsPage : Page
    {
        private Frame _mainFrame;
        public ObservableCollection<AuditLogItem> Logs { get; set; } = new();

        public LogsPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            AuditGrid.ItemsSource = Logs;
            _ = LoadLogs();
        }

        private async void ApplyFilters_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = EmailFilterBox.Text.Trim();
                string role = (RoleFilterBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                string description = DescriptionFilterBox.Text.Trim();
                string dateFrom = DateFromPicker.SelectedDate?.ToString("yyyy-MM-dd");
                string dateTo = DateToPicker.SelectedDate?.ToString("yyyy-MM-dd");

                string url = $"Logs/list?email={email}&role={role}&description={description}&dateFrom={dateFrom}&dateTo={dateTo}";
                var json = await ApiHelper.GetAsync<JsonElement[]>(url);

                Logs.Clear();
                foreach (var item in json)
                {
                    Logs.Add(new AuditLogItem
                    {
                        IdAudit = item.GetProperty("idAudit").GetInt32(),
                        Email = item.GetProperty("email").GetString(),
                        Role = item.GetProperty("role").GetString(),
                        Description = item.GetProperty("description").GetString(),
                        CreatedAt = item.GetProperty("createdAt").GetDateTime()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task LoadLogs()
        {
            try
            {
                Logs.Clear();

                string url = "Logs/list";
                var json = await ApiHelper.GetAsync<JsonElement[]>(url);

                foreach (var item in json)
                {
                    Logs.Add(new AuditLogItem
                    {
                        IdAudit = item.GetProperty("idAudit").GetInt32(),
                        Email = item.GetProperty("email").GetString(),
                        Role = item.GetProperty("role").GetString(),
                        Description = item.GetProperty("description").GetString(),
                        CreatedAt = item.GetProperty("createdAt").GetDateTime()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки логов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Reset_Click(object sender, RoutedEventArgs e)
        {
            EmailFilterBox.Text = "";
            RoleFilterBox.SelectedIndex = -1;
            DescriptionFilterBox.Text = "";
            DateFromPicker.SelectedDate = null;
            DateToPicker.SelectedDate = null;

            await LoadLogs();
        }

        private void ExportLogs_Click(object sender, RoutedEventArgs e)
        {
            if (AuditGrid.ItemsSource is not IEnumerable<AuditLogItem> logs || !logs.Any())
            {
                MessageBox.Show("Нет данных для экспорта.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Title = "Сохранить отчёт логов",
                Filter = "CSV файлы (*.csv)|*.csv",
                FileName = $"AuditLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                sb.AppendLine("ID;Email;Роль;Описание;Дата и время");

                foreach (var log in logs)
                {
                    sb.AppendLine($"{log.IdAudit};" +
                                  $"{log.Email};" +
                                  $"{log.Role};" +
                                  $"{log.Description.Replace(";", ",")};" +
                                  $"{log.CreatedAt:dd.MM.yyyy HH:mm:ss}");
                }

                File.WriteAllText(saveFileDialog.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show($"Файл успешно сохранён:\n{saveFileDialog.FileName}",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
    public class AuditLogItem
    {
        public int IdAudit { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


