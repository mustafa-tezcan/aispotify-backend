using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
    public class GptPlaylistResponseDto
    {
        [JsonPropertyName("songs")]
        public List<SongDto> Songs { get; set; } = new List<SongDto>();
    }
}