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

        public async Task<List<SpotifyTrackDto>> GetTrackDetailsAsync(
            List<(string artist, string track)> songs)
        {
            // 1️⃣ App-level token al (Client Credentials)
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

            // 2️⃣ Her şarkıyı ara
            var results = new List<SpotifyTrackDto>();

            foreach (var (artist, track) in songs)
            {
                var query = $"{track} artist:{artist}";
                var url = $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(query)}&type=track&limit=1";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) continue;

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var items = doc.RootElement.GetProperty("tracks").GetProperty("items");
                if (items.GetArrayLength() == 0) continue;

                var trackElement = items[0];
                results.Add(SpotifyMapper.ToSpotifyTrackDto(trackElement));
            }

            return results;
        }


        public Task<(bool Success, string NewAccessToken, string Error)> RefreshAccessTokenAsync(User user)
        {
            throw new NotImplementedException();
        }

        public async Task<string> CreatePlaylistAsync(
            string spotifyUserId, 
            string accessToken, 
            string name, 
            string description, 
            List<string> trackIds)
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken);

            // 1️⃣ Playlist oluştur
            var createPlaylistUrl = $"https://api.spotify.com/v1/users/{spotifyUserId}/playlists";
            var playlistData = new
            {
                name = name,
                description = description,
                @public = false // Private playlist
            };

            var createResponse = await _httpClient.PostAsJsonAsync(createPlaylistUrl, playlistData);
            
            if (!createResponse.IsSuccessStatusCode)
            {
                var error = await createResponse.Content.ReadAsStringAsync();
                throw new Exception($"Playlist oluşturulamadı: {error}");
            }

            var createJson = await createResponse.Content.ReadAsStringAsync();
            using var createDoc = JsonDocument.Parse(createJson);
            var playlistId = createDoc.RootElement.GetProperty("id").GetString();

            // 2️⃣ Şarkıları ekle
            var addTracksUrl = $"https://api.spotify.com/v1/playlists/{playlistId}/tracks";
            var trackUris = trackIds.Select(id => $"spotify:track:{id}").ToList();
            var tracksData = new { uris = trackUris };

            var addResponse = await _httpClient.PostAsJsonAsync(addTracksUrl, tracksData);
            
            if (!addResponse.IsSuccessStatusCode)
            {
                var error = await addResponse.Content.ReadAsStringAsync();
                throw new Exception($"Şarkılar eklenemedi: {error}");
            }

            Console.WriteLine($"✅ Playlist oluşturuldu: {playlistId}");
            return playlistId;
        }
    }
}