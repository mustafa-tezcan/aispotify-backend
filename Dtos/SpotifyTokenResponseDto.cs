using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace spotifyapp.Dtos
{
public class SpotifyTokenResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
}
}