// Services/AIService.cs
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using spotifyapp.Interfaces;

namespace spotifyapp.Services
{
    public class GptService : IGptService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GptService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        public async Task<string> CreatePlaylistAsync(string prompt)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);

            var requestData = new
            {
                model = "gpt-4o-mini",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = @"
                        Sen bir müzik öneri asistanısın.
                        Kullanıcıdan gelen ruh hali veya aktiviteye göre Spotify'da bulunabilecek şarkıları öner.
                        Cevabını mutlaka JSON formatında ver.
                        JSON içinde sadece 'songs' adlı bir dizi olsun.
                        Her eleman şu şekilde olmalı:
                        {
                            'artist': 'Sanatçı adı',
                            'track': 'Şarkı adı'
                        }
                        SADECE JSON döndür, hiçbir açıklama veya markdown kod bloğu kullanma.
                        Direkt olarak { ile başla.
                        "
                    },
                    new { role = "user", content = prompt }
                },
                temperature = 0.5
            };

            var response = await _httpClient.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", requestData);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GPT API isteği başarısız oldu: {err}");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            
            using var doc = JsonDocument.Parse(responseText);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            // Markdown kod bloklarını temizle (```json ... ``` veya ``` ... ```)
            var cleaned = CleanJsonResponse(content ?? "");
            
            return cleaned;
        }

        private string CleanJsonResponse(string response)
        {
            // Boşsa direkt döndür
            if (string.IsNullOrWhiteSpace(response))
                return response;

            // Markdown kod bloklarını kaldır: ```json veya ```
            var pattern = @"```(?:json)?\s*|\s*```";
            var cleaned = Regex.Replace(response, pattern, "").Trim();

            return cleaned;
        }
    }
}