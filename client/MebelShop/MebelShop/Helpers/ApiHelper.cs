using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using MebelShop.Auth;

namespace MebelShop.Helpers
{
    public static class ApiHelper
    {
        public static string BaseUrl { get; set; } = "http://localhost:5017/api/";
        public static string BaseImagesUrl { get; set; } = "http://localhost:5017/images/";

        private static HttpClient CreateClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(BaseUrl);

            var token = SettingsService.Settings.JwtToken;
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }

            return client;
        }

        public static async Task<T> GetAsync<T>(string endpoint)
        {
            using var client = CreateClient();
            var response = await client.GetAsync(endpoint);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<T>(json, options);
        }

        public static async Task<T> PostAsync<T>(string endpoint, object data)
        {
            using var client = CreateClient();
            var jsonData = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, jsonData);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }

        public static async Task<T> PutAsync<T>(string endpoint, object data)
        {
            using var client = CreateClient();
            var jsonData = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, jsonData);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }

        public static async Task<T> DeleteAsync<T>(string endpoint)
        {
            using var client = CreateClient();
            var response = await client.DeleteAsync(endpoint);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json);
        }

        public static async Task<string> DeleteAsync(string endpoint)
        {
            using var client = CreateClient();
            var response = await client.DeleteAsync(endpoint);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> PostAsync(string endpoint, object data)
        {
            using var client = CreateClient();
            var jsonData = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(endpoint, jsonData);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> PutAsync(string endpoint, object data)
        {
            using var client = CreateClient();
            var jsonData = new StringContent(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");
            var response = await client.PutAsync(endpoint, jsonData);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public static async Task<string> PostFileAsync(string endpoint, byte[] fileBytes, string fileName)
        {
            using var client = CreateClient();
            var formData = new MultipartFormDataContent();

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            formData.Add(fileContent, "file", fileName);

            var response = await client.PostAsync(endpoint, formData);

            HandleUnauthorized(response);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        private static void HandleUnauthorized(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                SettingsService.Settings.JwtToken = "";
                SettingsService.SaveSettings();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    var currentWindow = Application.Current.Windows
                        .OfType<Window>()
                        .FirstOrDefault(w => w.IsActive);

                    var authWindow = new AuthWindow();
                    authWindow.Show();

                    currentWindow?.Close();
                });
            }
        }
    }
}
