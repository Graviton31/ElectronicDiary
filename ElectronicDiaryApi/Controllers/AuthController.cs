using ElectronicDiaryApi.Data;
using ElectronicDiaryApi.Models;
using ElectronicDiaryApi.ModelsDto.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ElectronicDiaryApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ElectronicDiaryContext _context;
    private readonly IConfiguration _config;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(ElectronicDiaryContext context, IConfiguration config, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _config = config;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Login == request.Login && u.IsDelete != true);

        if (user == null)
            return Unauthorized(new { Message = "Неверный логин или пароль" });

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.Password, request.Password);

        if (verificationResult != PasswordVerificationResult.Success)
            return Unauthorized(new { Message = "Неверный логин или пароль" });

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenExpireDays"));
        await _context.SaveChangesAsync();

        // Возвращаем только access token клиенту
        return Ok(new
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.IdUser,
            Role = user.Role,
            FullName = $"{user.Surname} {user.Name} {user.Patronymic}",
            AccessTokenExpires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpireMinutes")),
            RefreshTokenExpires = user.RefreshTokenExpiryTime
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null) return BadRequest("Неверный токен");

        var userId = int.Parse(principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        var user = await _context.Users.FindAsync(userId);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            return BadRequest("Неверный refresh токен или срок его действия истёк");

        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTokenExpireDays"));
        await _context.SaveChangesAsync();

        return Ok(new
        {
            AccessToken = newAccessToken,
            AccessTokenExpires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpireMinutes"))
        });
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> RevokeToken()
    {
        var userId = int.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
        var user = await _context.Users.FindAsync(userId);

        if (user == null) return BadRequest();

        user.RefreshToken = null;
        user.RefreshTokenExpiryTime = null;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private string GenerateJwtToken(User user)
    {
        var secret = _config["Jwt:Key"];
        if (string.IsNullOrEmpty(secret))
        {
            throw new ArgumentNullException("Jwt:Key", "JWT secret key is not configured");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.IdUser.ToString()),
            new Claim(ClaimTypes.Name, user.Login),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("FullName", $"{user.Surname} {user.Name} {user.Patronymic}")
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:ExpireMinutes")),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"])),
            ValidateIssuer = true,
            ValidIssuer = _config["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _config["Jwt:Audience"],
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            return null;

        return principal;
    }
}