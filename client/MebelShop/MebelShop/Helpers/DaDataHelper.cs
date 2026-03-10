using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MebelShop.Helpers
{
    public static class DaDataHelper
    {
        private static readonly HttpClient client = new HttpClient();
        private const string apiKey = "9e87487c1c002995b49a48e87740b4911fef40a2";

        public static async Task<List<string>> GetAddressSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return new List<string>();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://suggestions.dadata.ru/suggestions/api/4_1/rs/suggest/address");
            request.Headers.Authorization = new AuthenticationHeaderValue("Token", apiKey);

            var content = new { query = query, count = 5 };
            request.Content = new StringContent(JsonSerializer.Serialize(content));
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            var result = await JsonSerializer.DeserializeAsync<DaDataResponse>(stream);

            return result?.suggestions?.Select(s => s.value).ToList() ?? new List<string>();
        }
    }

    public class DaDataResponse
    {
        public List<DaDataSuggestion> suggestions { get; set; }
    }

    public class DaDataSuggestion
    {
        public string value { get; set; }
    }
}
