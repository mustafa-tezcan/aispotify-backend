using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using spotifyapp.Dtos;
using spotifyapp.Models;

namespace spotifyapp.Interfaces
{
    public interface ISpotifyService
    {
        Task<String> GetCurrentUserProfile(string accessToken);
        Task<string> CreatePlaylist(string accessToken, string userId, string playlistName, string description);

        // Var olan bir playliste şarkı ekler
        Task<string> AddTrackToPlaylistAsync(string accessToken, string playlistId, string trackUri);

        // Access Token yeniler (refresh token ile)
        Task<(bool Success, string NewAccessToken, string Error)> RefreshAccessTokenAsync(User user);

        //kullanıcı onay verince dönen kodu kullarak acces token , refresh token vs almak için .
        Task<string> ExchangeCodeForTokenAsync(string code);

        public Task<List<SpotifyTrackDto>> GetTrackDetailsAsync(List<(string artist, string track)> songs);
        
    }
}