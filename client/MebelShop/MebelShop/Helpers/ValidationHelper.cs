using System.Text.RegularExpressions;

namespace MebelShop.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            bool hasUpper = password.Any(c => char.IsUpper(c));
            bool hasLower = password.Any(c => char.IsLower(c));
            bool hasDigit = password.Any(c => char.IsDigit(c));
            return hasUpper && hasLower && hasDigit;
        }
    }
}
