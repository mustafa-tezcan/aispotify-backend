using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
    public class SpotifyUserProfileDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfileUrl { get; set; } = string.Empty; // external_urls.spotify
        public int Followers { get; set; }
        public string ImageUrl { get; set; } = string.Empty; // İlk resmi alabiliriz

    }

}