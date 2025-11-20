using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using spotifyapp.Interfaces;
using spotifyapp.Mappers;

namespace spotifyapp.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ISpotifyService _spotifyService;
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration; //appsettings.json’daki ClientId, ClientSecret, RedirectUri gibi ayarlara erişim sağlar
        private readonly IHttpClientFactory _httpClientFactory; //Spotify API’ye HTTP istekleri yapmak için HttpClient nesnelerini üretir ve yönetir

        private readonly ITokenService _tokenService;


        public AuthController(IUserRepository userRepository, IConfiguration configuration, IHttpClientFactory httpClientFactory, ISpotifyService spotifyService, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _spotifyService = spotifyService;
            _tokenService = tokenService;
        }

        [HttpGet("login")]
        public IActionResult Login()
        {
            var clientId = _configuration["Spotify:ClientId"];
            var redirectUri = _configuration["Spotify:RedirectUri"];

            if (string.IsNullOrEmpty(redirectUri))
                return BadRequest("Redirect URI is not configured.");

            var scope = "user-read-email playlist-read-private playlist-modify-private";

            // 🔹 State üret
            var state = Guid.NewGuid().ToString();
            // İstersen session veya cache’de saklayabilirsin, callback’te doğrulamak için

            var spotifyAuthUrl =
                "https://accounts.spotify.com/authorize" +
                "?response_type=code" +
                $"&client_id={clientId}" +
                $"&scope={Uri.EscapeDataString(scope)}" +
                $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                $"&state={state}";

            return Redirect(spotifyAuthUrl);
        }

        [HttpGet("callback")]
        public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest("Authorization code is missing.");

            if (string.IsNullOrEmpty(state))
                return BadRequest("State parameter is required.");

            // 🔹 Burada login sırasında sakladığın state ile eşleştir
            // Örnek: if (state != expectedState) return BadRequest("State mismatch");

            try
            {
                // 1️⃣ Spotify'dan access & refresh token al
                var tokenResponseJson = await _spotifyService.ExchangeCodeForTokenAsync(code);
                var tokenDto = SpotifyMapper.ToTokenDto(tokenResponseJson);

                if (string.IsNullOrEmpty(tokenDto.AccessToken))
                    return BadRequest("Access token could not be retrieved.");

                // 2️⃣ Kullanıcı profilini al
                var userProfileJson = await _spotifyService.GetCurrentUserProfile(tokenDto.AccessToken);
                var userDto = SpotifyMapper.ToUserProfileDto(userProfileJson);

                // 3️⃣ User modelini oluştur
                var userModel = SpotifyMapper.ToUserModel(userDto, tokenDto);

                // 4️⃣ Database'e kaydet veya güncelle
                var existingUser = await _userRepository.GetBySpotifyIdAsync(userModel.SpotifyId);
                if (existingUser == null)
                {
                    await _userRepository.CreateAsync(userModel);
                }
                else
                {
                    existingUser.DisplayName = userModel.DisplayName;
                    existingUser.Email = userModel.Email;
                    existingUser.AccessToken = userModel.AccessToken;
                    existingUser.RefreshToken = userModel.RefreshToken;
                    existingUser.TokenExpiressAt = userModel.TokenExpiressAt;
                    existingUser.ProfileImageUrl = userModel.ProfileImageUrl;
                    existingUser.Followers = userModel.Followers;
                    await _userRepository.UpdateAsync(existingUser);
                }

                // 5️⃣ Sonucu dön
                return Ok(new
                {
                    message = "Spotify bağlantısı başarılı!",
                    token = tokenDto,
                    profile = userDto,
                    jwt = _tokenService.CreateToken(existingUser)
                });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(500, $"Spotify API hatası: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Beklenmeyen hata: {ex.Message}");
            }
        }
        
        
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            // ✅ Debug: Tüm claim'leri göster
            var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            Console.WriteLine($"All claims: {string.Join(", ", allClaims.Select(c => $"{c.Type}={c.Value}"))}");

            // ✅ ClaimTypes.NameIdentifier ile nameid aynı değil!
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Ok(new { message = "NameIdentifier bulunamadı", allClaims });
            }

            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Ok(new { message = "Parse hatası", value = userIdClaim });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            { 
                user.Id,
                user.DisplayName,
                user.SpotifyId,
                user.Email,
                user.ProfileImageUrl,
                user.Followers
            });
        }
    }
}