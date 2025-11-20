
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
    public class SongDto
    {
        [JsonPropertyName("artist")]
        public string Artist { get; set; } = string.Empty;
        
        [JsonPropertyName("track")]
        public string Track { get; set; } = string.Empty;
    }
}