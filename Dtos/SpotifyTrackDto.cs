using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
    public class SpotifyTrackDto
    {
        public string Artist { get; set; } = string.Empty;
        public string Track { get; set; } = string.Empty;
        public string AlbumImage { get; set; } = string.Empty;
        public string PreviewUrl { get; set; } = string.Empty; 
        public string SpotifyUrl { get; set; } = string.Empty;
    }
}