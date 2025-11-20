using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using spotifyapp.Dtos;
using spotifyapp.Interfaces;
using spotifyapp.Services;

namespace spotifyapp.Controllers
{
    [Route("api/playlist")]
    [ApiController]
    public class PlayListController : Controller
    {
        private readonly IGptService _gptService;
        private readonly ISpotifyService _spotifyService;
        public PlayListController(IGptService gptService , ISpotifyService spotifyService)
        {
            _gptService = gptService;
            _spotifyService = spotifyService;
        }

        [HttpGet("suggest")]
        public async Task<IActionResult> SuggestPlaylist(string prompt)
        {
            // 1️⃣ GPT'den JSON string al
            var json = await _gptService.CreatePlaylistAsync(prompt);

            Console.WriteLine("=== CONTROLLER - GELEN JSON ===");
            Console.WriteLine(json);
            Console.WriteLine("=== END ===");

            // 2️⃣ JSON'u parse et
            GptPlaylistResponseDto gptResponse = null;
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                gptResponse = JsonSerializer.Deserialize<GptPlaylistResponseDto>(json, options);
                
                Console.WriteLine("=== PARSE BAŞARILI ===");
                Console.WriteLine($"gptResponse null mu? {gptResponse == null}");
                Console.WriteLine($"Songs null mu? {gptResponse?.Songs == null}");
                Console.WriteLine($"Songs Count: {gptResponse?.Songs?.Count ?? 0}");
                Console.WriteLine("=== END ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== PARSE HATASI ===");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("=== END ===");
                return BadRequest($"JSON parse hatası: {ex.Message}");
            }

            var songs = gptResponse?.Songs ?? new List<SongDto>();

            if (songs.Count == 0)
            {
                Console.WriteLine("=== UYARI: Songs listesi boş! ===");
                return BadRequest("GPT'den geçerli şarkı gelmedi.");
            }

            // 3️⃣ Spotify detaylarını al
            var spotifySongs = await _spotifyService.GetTrackDetailsAsync(
                songs.Select(s => (s.Artist, s.Track)).ToList()
            );

            // 4️⃣ JSON olarak döndür
            return Ok(new
            {
                success = true,
                data = spotifySongs
            });
        }
    }
}