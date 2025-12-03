using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
    public class ExportPlaylistRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tracks { get; set; } = new(); // Spotify Track ID'leri
}
}