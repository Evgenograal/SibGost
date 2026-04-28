using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace StaffApp.Services
{
    public class ImgBBService
    {

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string ApiUrl = "https://api.imgbb.com/1/upload";

        public ImgBBService(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<string?> UploadAsync(byte[] imageBytes, string fileName)
        {
            try
            {
                string base64 = Convert.ToBase64String(imageBytes);

                var content = new FormUrlEncodedContent(new[]
                {
                new KeyValuePair<string, string>("key", _apiKey),
                new KeyValuePair<string, string>("image", base64),
                new KeyValuePair<string, string>("name", fileName)
            });

                var response = await _httpClient.PostAsync(ApiUrl, content);
                var json = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.GetProperty("success").GetBoolean())
                {
                    return root.GetProperty("data").GetProperty("url").GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Upload error: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> UploadFromPathAsenc(string filePath) 
        {
            if(!File.Exists(filePath)) return null;

            var bytes = await File.ReadAllBytesAsync(filePath);
            var name = Path.GetFileName(filePath);
            return await UploadAsync(bytes, name);
        }
    }
}
