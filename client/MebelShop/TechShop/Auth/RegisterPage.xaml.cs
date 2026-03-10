using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;
using MebelShop.Models;

namespace MebelShop.Auth
{
    public partial class RegisterPage : Page
    {
        private Frame _mainFrame;
        private string _email;
        private string _password;

        public RegisterPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private async void Step1Next_Click(object sender, RoutedEventArgs e)
        {
            _email = EmailTextBox.Text.Trim();
            _password = PasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (!ValidationHelper.IsValidEmail(_email))
            {
                MessageBox.Show("Введите корректный Email", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ValidationHelper.IsValidPassword(_password))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов, хотя бы одну заглавную, одну строчную и цифру", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_password != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/send-registration-code", new { Email = _email, Password = _password });
                var root = JsonDocument.Parse(responseJson).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Step1Panel.Visibility = Visibility.Collapsed;
                Step2Panel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Step2Confirm_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeTextBox.Text.Trim();

            if (!Regex.IsMatch(code, @"^\d{6}$"))
            {
                MessageBox.Show("Введите код из 6 цифр", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/verify-registration-code", new { Email = _email, Code = code });
                var root = JsonDocument.Parse(responseJson).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Step2Panel.Visibility = Visibility.Collapsed;
                Step3Panel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Step3Finish_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameTextBox.Text.Trim();
            string lastName = LastNameTextBox.Text.Trim();

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/finalize-registration", new
                {
                    Email = _email,
                    Password = _password,
                    FirstName = firstName,
                    LastName = lastName
                });

                var root = JsonDocument.Parse(responseJson).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show("Регистрация завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService?.Navigate(new LoginPage(_mainFrame));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new LoginPage(_mainFrame));
        }
    }
}
