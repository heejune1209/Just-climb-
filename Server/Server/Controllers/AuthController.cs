using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Server.Services;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;
        private readonly HttpClient _httpClient;

        public AuthController(
            IConfiguration configuration,
            IUserService userService,
            ILogger<AuthController> logger,
            HttpClient httpClient)
        {
            _configuration = configuration;
            _userService = userService;
            _logger = logger;
            _httpClient = httpClient;
        }

        [HttpPost("steam")]
        public async Task<IActionResult> AuthenticateWithSteam([FromBody] SteamAuthRequest request)
        {
            try
            {
                _logger.LogInformation("Steam authentication request received for SteamID: {SteamId}, DisplayName: {DisplayName}", 
                    request.SteamId, request.SteamDisplayName);

                // 1. Steam Web API를 사용해서 티켓 검증
                bool isValidTicket = await ValidateSteamTicket(request.SteamId, request.AuthTicket);
                if (!isValidTicket)
                {
                    _logger.LogWarning("Invalid Steam ticket for SteamID: {SteamId}", request.SteamId);
                    return BadRequest(new AuthResponse 
                    { 
                        Success = false, 
                        Message = "Invalid Steam authentication ticket" 
                    });
                }

                // 2. 사용자 정보 가져오기 또는 생성 (Steam 닉네임 포함)
                var user = await _userService.GetOrCreateUserAsync(request.SteamId, request.SteamDisplayName);
                if (user == null)
                {
                    _logger.LogError("Failed to get or create user for SteamID: {SteamId}", request.SteamId);
                    return StatusCode(500, new AuthResponse 
                    { 
                        Success = false, 
                        Message = "Failed to create user account" 
                    });
                }

                // 3. JWT 토큰 생성
                string jwtToken = GenerateJwtToken(user);

                _logger.LogInformation("Steam authentication successful for SteamID: {SteamId}, DisplayName: {DisplayName}", 
                    request.SteamId, request.SteamDisplayName);
                
                return Ok(new AuthResponse 
                { 
                    Success = true, 
                    Token = jwtToken,
                    Message = "Authentication successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Steam authentication for SteamID: {SteamId}", request.SteamId);
                return StatusCode(500, new AuthResponse 
                { 
                    Success = false, 
                    Message = "Internal server error" 
                });
            }
        }

        private async Task<bool> ValidateSteamTicket(string steamId, string authTicket)
        {
            try
            {
                // Steam Web API 설정
                var steamWebApiKey = _configuration["SteamSettings:WebApiKey"];
                var steamAppId = _configuration["SteamSettings:AppId"];

                if (string.IsNullOrEmpty(steamWebApiKey))
                {
                    _logger.LogError("Steam Web API Key is not configured");
                    return false;
                }

                if (string.IsNullOrEmpty(steamAppId))
                {
                    _logger.LogError("Steam App ID is not configured");
                    return false;
                }

                // Steam Web API 호출
                var steamApiUrl = $"https://api.steampowered.com/ISteamUserAuth/AuthenticateUserTicket/v1/";
                var steamApiParams = new Dictionary<string, string>
                {
                    { "key", steamWebApiKey },
                    { "appid", steamAppId },
                    { "ticket", authTicket }
                };

                var formContent = new FormUrlEncodedContent(steamApiParams);
                var response = await _httpClient.PostAsync(steamApiUrl, formContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Steam API request failed with status: {StatusCode}", response.StatusCode);
                    return false;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Steam API response: {Response}", responseContent);

                // JSON 응답 파싱
                using var jsonDoc = JsonDocument.Parse(responseContent);
                var responseElement = jsonDoc.RootElement.GetProperty("response");
                
                if (responseElement.TryGetProperty("error", out var errorElement))
                {
                    var errorMessage = errorElement.GetProperty("errordesc").GetString();
                    _logger.LogWarning("Steam API error: {Error}", errorMessage);
                    return false;
                }

                if (responseElement.TryGetProperty("params", out var paramsElement))
                {
                    var steamIdFromApi = paramsElement.GetProperty("steamid").GetString();
                    
                    // Steam ID 일치 확인
                    if (steamIdFromApi != steamId)
                    {
                        _logger.LogWarning("Steam ID mismatch. Expected: {Expected}, Got: {Actual}", 
                            steamId, steamIdFromApi);
                        return false;
                    }

                    _logger.LogInformation("Steam ticket validation successful for SteamID: {SteamId}", steamId);
                    return true;
                }

                _logger.LogError("Invalid Steam API response format");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Steam ticket for SteamID: {SteamId}", steamId);
                return false;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Id),
                new Claim("steam_id", user.Id),
                new Claim("user_id", user.Id)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class SteamAuthRequest
    {
        public string SteamId { get; set; } = string.Empty;
        public string AuthTicket { get; set; } = string.Empty;
        public string SteamDisplayName { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
} 