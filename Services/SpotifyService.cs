using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using spotifyapp.Dtos;
using spotifyapp.Interfaces;
using spotifyapp.Mappers;
using spotifyapp.Models;

namespace spotifyapp.Services
{
    public class SpotifyService : ISpotifyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SpotifyService(IHttpClientFactory httpClientFactory , IConfiguration configuration , HttpClient httpClient)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpClient = httpClient;

        }


        public Task<string> AddTrackToPlaylistAsync(string accessToken, string playlistId, string trackUri)
        {
            throw new NotImplementedException();
        }

        public Task<string> CreatePlaylist(string accessToken, string userId, string playlistName, string description)
        {
            throw new NotImplementedException();
        }


        public async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];
            var redirectUri = _configuration["Spotify:RedirectUri"];

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(redirectUri))
                throw new InvalidOperationException("Spotify credentials or redirect URI are not configured properly.");

            var client = _httpClientFactory.CreateClient();

            var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            var body = new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri }
            };
            request.Content = new FormUrlEncodedContent(body);

            // Authorization header (Basic base64(clientId:clientSecret))
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);

            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Spotify token request failed. Status: {response.StatusCode}, Body: {responseBody}");

            return responseBody; // access_token, refresh_token, expires_in vs.
        }

        public async Task<string> GetCurrentUserProfile(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("spotify api request failed" + response.StatusCode + "  " + content);

            }
            return content;
        }

   public async Task<List<SpotifyTrackDto>> GetTrackDetailsAsync(List<(string artist, string track)> songs)
{
    // 1️⃣ App-level token al
    var clientId = _configuration["Spotify:ClientId"];
    var clientSecret = _configuration["Spotify:ClientSecret"];
    var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
    tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
    tokenRequest.Content = new FormUrlEncodedContent(new[]
    {
        new KeyValuePair<string, string>("grant_type", "client_credentials")
    });

    var tokenResponse = await _httpClient.SendAsync(tokenRequest);
    tokenResponse.EnsureSuccessStatusCode();
    var tokenJson = await tokenResponse.Content.ReadAsStringAsync();
    using var tokenDoc = JsonDocument.Parse(tokenJson);
    var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

    // 2️⃣ Her şarkıyı Spotify Search API ile detaylandır
    var results = new List<SpotifyTrackDto>();

    foreach (var (artist, track) in songs)
    {
        var query = $"{track} artist:{artist}";
        var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1";

        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) continue;

        var json = await response.Content.ReadAsStringAsync();
        
        // DEBUG: Spotify'den gelen raw response
        Console.WriteLine($"=== SPOTIFY RAW RESPONSE for {track} ===");
        Console.WriteLine(json);
        Console.WriteLine("=== END ===");
        
        using var doc = JsonDocument.Parse(json);

        var items = doc.RootElement.GetProperty("tracks").GetProperty("items");
        if (items.GetArrayLength() == 0) continue;

        var trackElement = items[0];
        
        // DEBUG: preview_url field'ını kontrol et
        if (trackElement.TryGetProperty("preview_url", out var previewProp))
        {
            Console.WriteLine($"Track: {track}");
            Console.WriteLine($"  preview_url exists: YES");
            Console.WriteLine($"  preview_url is null: {previewProp.ValueKind == JsonValueKind.Null}");
            Console.WriteLine($"  preview_url value: {previewProp.GetString() ?? "NULL"}");
        }
        else
        {
            Console.WriteLine($"Track: {track} - preview_url field YOK!");
        }

        results.Add(SpotifyMapper.ToSpotifyTrackDto(trackElement));
    }

    return results;
}


        public Task<(bool Success, string NewAccessToken, string Error)> RefreshAccessTokenAsync(User user)
        {
            throw new NotImplementedException();
        }
    }
}