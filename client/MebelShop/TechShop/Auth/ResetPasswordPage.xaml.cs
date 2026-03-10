using System;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using MebelShop.Helpers;
using MebelShop.Models;

namespace MebelShop.Auth
{
    public partial class ResetPasswordPage : Page
    {
        private Frame _mainFrame;
        private string _email;
        public ResetPasswordPage(Frame mainFrame)
        {
            InitializeComponent();
            _mainFrame = mainFrame;
        }

        private async void SendCode_Click(object sender, RoutedEventArgs e)
        {
            _email = EmailTextBox.Text.Trim();

            if (string.IsNullOrEmpty(_email) || !Regex.IsMatch(_email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                MessageBox.Show("Введите корректный email.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/send-reset-code", new { Email = _email });
                var root = JsonDocument.Parse(responseJson).RootElement;

                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                StepEmail.Visibility = Visibility.Collapsed;
                StepCode.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void VerifyCode_Click(object sender, RoutedEventArgs e)
        {
            string code = CodeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(code) || code.Length != 6)
            {
                MessageBox.Show("Введите 6-значный код.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/verify-reset-code", new { Email = _email, Code = code });
                var root = JsonDocument.Parse(responseJson).RootElement;
                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                StepCode.Visibility = Visibility.Collapsed;
                StepPassword.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SavePassword_Click(object sender, RoutedEventArgs e)
        {
            string newPassword = NewPasswordBox.Password;
            string confirmPassword = ConfirmPasswordBox.Password;

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Regex.IsMatch(newPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                MessageBox.Show("Пароль должен содержать минимум 8 символов, одну заглавную и одну строчную букву, и хотя бы одну цифру.",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var responseJson = await ApiHelper.PostAsync("auth/change-password", new { Email = _email, NewPassword = newPassword });
                var root = JsonDocument.Parse(responseJson).RootElement;
                if (!root.GetProperty("success").GetBoolean())
                {
                    MessageBox.Show(root.GetProperty("message").GetString(), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                MessageBox.Show("Пароль успешно изменён!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    _mainFrame.Navigate(new LoginPage(_mainFrame));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запроса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            _mainFrame.Navigate(new LoginPage(_mainFrame));
        }
    }
}
