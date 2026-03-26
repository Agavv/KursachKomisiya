using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MebelShop.Helpers;

namespace MebelShop.Director
{
    public partial class EmployeesPage : Page
    {
        private Frame _mainFrame;
        private UserViewModel? _selectedUser;
        private bool _isNewUser = false;
        private ObservableCollection<UserViewModel> AllUsers { get; set; } = new();
        public ObservableCollection<UserViewModel> FilteredUsers { get; set; } = new();

        private readonly DispatcherTimer _searchTimer;

        public EmployeesPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
            UsersGrid.ItemsSource = FilteredUsers;

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ApplyFilters();
            };

            EmailFilterBox.TextChanged += FilterBox_TextChanged;
            FirstNameFilterBox.TextChanged += FilterBox_TextChanged;
            SurnameFilterBox.TextChanged += FilterBox_TextChanged;
            IdFilterBox.TextChanged += (s, e) => ApplyFilters();
            RoleFilterBox.SelectionChanged += (s, e) => ApplyFilters();

            _ = LoadUsersAsync();
        }

        public async Task LoadUsersAsync()
        {
            try
            {
                string url = "Users/list";
                var json = await ApiHelper.GetAsync<JsonElement[]>(url);

                AllUsers.Clear();
                foreach (var u in json)
                {
                    AllUsers.Add(new UserViewModel
                    {
                        IdUser = u.GetProperty("idUser").GetInt32(),
                        Email = u.GetProperty("email").GetString(),
                        Role = u.GetProperty("role").GetString(),
                        FirstName = u.GetProperty("firstName").GetString(),
                        Surname = u.GetProperty("surname").GetString()
                    });
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilters()
        {
            string idText = IdFilterBox.Text.Trim().ToLower();
            string email = EmailFilterBox.Text.Trim().ToLower();
            string role = ((ComboBoxItem)RoleFilterBox.SelectedItem).Content.ToString();
            string firstName = FirstNameFilterBox.Text.Trim().ToLower();
            string surname = SurnameFilterBox.Text.Trim().ToLower();

            var filtered = AllUsers.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(idText) && int.TryParse(idText, out int id))
                filtered = filtered.Where(u => u.IdUser == id);

            if (email.Length >= 2)
                filtered = filtered.Where(u => u.Email?.ToLower().Contains(email) == true);

            if (role != "Все сотрудники")
                filtered = filtered.Where(u => u.Role?.ToLower() == role.ToLower());

            if (firstName.Length >= 2)
                filtered = filtered.Where(u => u.FirstName?.ToLower().Contains(firstName) == true);

            if (surname.Length >= 2)
                filtered = filtered.Where(u => u.Surname?.ToLower().Contains(surname) == true);

            FilteredUsers.Clear();
            foreach (var u in filtered)
                FilteredUsers.Add(u);
        }

        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            IdFilterBox.Text = "";
            EmailFilterBox.Text = "";
            RoleFilterBox.SelectedIndex = 0;
            FirstNameFilterBox.Text = "";
            SurnameFilterBox.Text = "";
            ApplyFilters();
        }

        private async void UsersGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (UsersGrid.SelectedItem is UserViewModel user)
            {
                _selectedUser = user;
                EditEmailBox.Text = user.Email;
                EditFirstNameBox.Text = user.FirstName;
                EditSurnameBox.Text = user.Surname;

                var roleItem = EditRoleBox.Items.Cast<ComboBoxItem>()
                    .FirstOrDefault(i => i.Content.ToString() == user.Role);
                if (roleItem != null)
                    EditRoleBox.SelectedItem = roleItem;

                //await DialogHost.Show(EditDialog, "RootDialog");
            }
        }

        private async void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = EditEmailBox.Text.Trim();
                string firstName = EditFirstNameBox.Text.Trim();
                string surname = EditSurnameBox.Text.Trim();
                string password = EditPasswordBox.Password;
                string role = ((ComboBoxItem)EditRoleBox.SelectedItem).Content.ToString();

                if (firstName.Length < 2 || surname.Length < 2)
                {
                    MessageBox.Show("Имя и фамилия должны содержать минимум 2 буквы.",
                        "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    MessageBox.Show("Введите корректный адрес электронной почты.",
                        "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_isNewUser || !string.IsNullOrWhiteSpace(password))
                {
                    if (password.Length < 8 ||
                        !Regex.IsMatch(password, @"[A-Z]") ||
                        !Regex.IsMatch(password, @"[a-z]") ||
                        !Regex.IsMatch(password, @"\d"))
                    {
                        MessageBox.Show("Пароль должен содержать минимум 8 символов, включая хотя бы 1 заглавную, 1 строчную букву и 1 цифру.",
                            "Ошибка ввода", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                var data = new
                {
                    Email = email,
                    Role = role,
                    FirstName = firstName,
                    Surname = surname,
                    Password = string.IsNullOrWhiteSpace(password) ? null : password
                };

                if (_isNewUser)
                {
                    await ApiHelper.PostAsync("Users/create", data);
                }
                else if (_selectedUser != null)
                {
                    await ApiHelper.PutAsync($"Users/update/{_selectedUser.IdUser}", data);
                    _selectedUser.Email = data.Email;
                    _selectedUser.Role = data.Role;
                    _selectedUser.FirstName = data.FirstName;
                    _selectedUser.Surname = data.Surname;
                }

                EmployeesDialogHost.IsOpen = false;
                await LoadUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null)
            {
                MessageBox.Show("Выберите пользователя для удаления.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string json = await ApiHelper.DeleteAsync($"Users/delete/{_selectedUser.IdUser}");

                var parsed = JsonDocument.Parse(json).RootElement;
                bool confirm = parsed.GetProperty("confirm").GetBoolean();
                string message = parsed.GetProperty("message").GetString();

                if (confirm)
                {
                    string formattedMessage = message.Replace("\\n", "\n").Trim();

                    var result = MessageBox.Show(
                        formattedMessage + "\n\nВы уверены, что хотите продолжить?",
                        "Подтверждение удаления",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        await ApiHelper.DeleteAsync($"Users/confirm-delete/{_selectedUser.IdUser}");
                        MessageBox.Show("Пользователь и все связанные данные удалены.", "Успешно",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        EmployeesDialogHost.IsOpen = false;
                        await LoadUsersAsync();
                    }
                }
                else
                {
                    MessageBox.Show("Ошибка при получении информации о пользователе.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeUser_Click(object sender, RoutedEventArgs e)
        {
            UsersGrid_MouseDoubleClick(this, null);
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            _isNewUser = true;
            _selectedUser = null;

            DialogTitle.Text = "Добавление нового пользователя";
            EditEmailBox.Text = "";
            EditFirstNameBox.Text = "";
            EditSurnameBox.Text = "";
            EditPasswordBox.Password = "";
            EditRoleBox.SelectedIndex = 0;

            EmployeesDialogHost.IsOpen = true;
        }

        private void UsersDataGrid_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(UsersGrid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row != null)
            {
                UsersGrid.SelectedItem = row.Item;
                _selectedUser = row.Item as UserViewModel;
            }
        }
    }

    public class UserViewModel : INotifyPropertyChanged
    {
        private int _idUser;
        private string _email;
        private string _role;
        private string _firstName;
        private string _surname;

        public int IdUser { get => _idUser; set { _idUser = value; OnPropertyChanged(nameof(IdUser)); } }
        public string Email { get => _email; set { _email = value; OnPropertyChanged(nameof(Email)); } }
        public string Role { get => _role; set { _role = value; OnPropertyChanged(nameof(Role)); } }
        public string FirstName { get => _firstName; set { _firstName = value; OnPropertyChanged(nameof(FirstName)); } }
        public string Surname { get => _surname; set { _surname = value; OnPropertyChanged(nameof(Surname)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
