using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using spotifyapp.Dtos;
using spotifyapp.Models;

namespace spotifyapp.Mappers
{
    public static class SpotifyMapper 
    {
        public static SpotifyTokenResponseDto ToTokenDto(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new SpotifyTokenResponseDto
            {
                AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
                TokenType = root.GetProperty("token_type").GetString() ?? string.Empty,
                ExpiresIn = root.GetProperty("expires_in").GetInt32(),
                RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
                Scope = root.GetProperty("scope").GetString() ?? string.Empty
            };
        }

        public static SpotifyUserProfileDto ToUserProfileDto(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            string profileUrl = root.GetProperty("external_urls").GetProperty("spotify").GetString() ?? string.Empty;

            int followers = 0;
            if (root.TryGetProperty("followers", out var followersProp) &&
                followersProp.TryGetProperty("total", out var totalProp))
            {
                followers = totalProp.GetInt32();
            }

            string imageUrl = string.Empty;
            if (root.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
            {
                imageUrl = images[0].GetProperty("url").GetString() ?? string.Empty;
            }

            return new SpotifyUserProfileDto
            {
                Id = root.GetProperty("id").GetString() ?? string.Empty,
                DisplayName = root.GetProperty("display_name").GetString() ?? string.Empty,
                Email = root.GetProperty("email").GetString() ?? string.Empty,
                ProfileUrl = profileUrl,
                Followers = followers,
                ImageUrl = imageUrl
            };
        }

        public static User ToUserModel(SpotifyUserProfileDto userDto, SpotifyTokenResponseDto tokenDto)
        {
            return new User
            {
                SpotifyId = userDto.Id,
                DisplayName = userDto.DisplayName,
                Email = userDto.Email,
                AccessToken = tokenDto.AccessToken,
                RefreshToken = tokenDto.RefreshToken,
                TokenExpiressAt = DateTime.UtcNow.AddSeconds(tokenDto.ExpiresIn),
                ProfileImageUrl = userDto.ImageUrl,
                Followers = userDto.Followers
            };
        }
        
        public static SpotifyTrackDto ToSpotifyTrackDto(JsonElement trackElement)
        {
            var artist = trackElement.GetProperty("artists")[0].GetProperty("name").GetString() ?? string.Empty;
            var trackName = trackElement.GetProperty("name").GetString() ?? string.Empty;

            string albumImage = string.Empty;
            var images = trackElement.GetProperty("album").GetProperty("images");
            if (images.GetArrayLength() > 0)
            {
                albumImage = images[0].GetProperty("url").GetString() ?? string.Empty;
            }

            string previewUrl = string.Empty;
            if (trackElement.TryGetProperty("preview_url", out var previewElement) 
                && previewElement.ValueKind != JsonValueKind.Null)
            {
                previewUrl = previewElement.GetString() ?? string.Empty;
            }
            var spotifyUrl = trackElement.GetProperty("external_urls").GetProperty("spotify").GetString() ?? string.Empty;

            return new SpotifyTrackDto
            {
                Artist = artist,
                Track = trackName,
                AlbumImage = albumImage,
                PreviewUrl = previewUrl,
                SpotifyUrl = spotifyUrl
            };
        }
    }
}