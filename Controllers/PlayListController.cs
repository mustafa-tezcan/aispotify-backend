using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotifyapp.Dtos;
using spotifyapp.Interfaces;


namespace spotifyapp.Controllers
{
    [Route("api/playlist")]
    [ApiController]
    public class PlayListController : Controller
    {
        private readonly IGptService _gptService;
        private readonly ISpotifyService _spotifyService;
        private readonly IUserRepository _userRepository;
        public PlayListController(IGptService gptService , ISpotifyService spotifyService , IUserRepository userRepository)
        {
            _gptService = gptService;
            _spotifyService = spotifyService;
            _userRepository = userRepository;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> SuggestPlaylist(string prompt)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized();
            
            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.AccessToken == null)
                return Unauthorized("Spotify token bulunamadı");
            
            var json = await _gptService.CreatePlaylistAsync(prompt);
            var gptResponse = JsonSerializer.Deserialize<GptPlaylistResponseDto>(json);
            var songs = gptResponse?.Songs ?? new List<SongDto>();
            
            // ✅ User token gönder
            var spotifySongs = await _spotifyService.GetTrackDetailsAsync(
                songs.Select(s => (s.Artist, s.Track)).ToList()
            );
            
            return Ok(new { success = true, data = spotifySongs });
        }
[HttpPost("export")]
[Authorize]
public async Task<IActionResult> ExportToSpotify([FromBody] ExportPlaylistRequestDto request)
{
    Console.WriteLine("=== EXPORT REQUEST ===");
    Console.WriteLine($"Name: {request.Name}");
    Console.WriteLine($"Description: {request.Description}");
    Console.WriteLine($"Tracks Count: {request.Tracks?.Count ?? 0}");
    Console.WriteLine("=== END ===");

    var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
    {
        Console.WriteLine("❌ USER ID ALINAMADI");
        return Unauthorized("Kullanıcı bulunamadı");
    }
    
    var user = await _userRepository.GetByIdAsync(userId);
    Console.WriteLine($"User found: {user != null}");
    Console.WriteLine($"Access Token: {user?.AccessToken?.Substring(0, 20)}...");
    
    if (user == null || string.IsNullOrEmpty(user.AccessToken))
    {
        Console.WriteLine("❌ USER TOKEN YOK");
        return Unauthorized("Spotify bağlantısı bulunamadı");
    }
    
    try
    {
        var playlistId = await _spotifyService.CreatePlaylistAsync(
            user.SpotifyId,
            user.AccessToken,
            request.Name,
            request.Description,
            request.Tracks
        );
        
        Console.WriteLine($"✅ SUCCESS: Playlist ID = {playlistId}");
        
        return Ok(new
        {
            success = true,
            message = "Playlist başarıyla oluşturuldu!",
            playlistId = playlistId
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ EXCEPTION: {ex.Message}");
        Console.WriteLine($"Stack: {ex.StackTrace}");
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}
    }
    
}