using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BusinessLogic.Services;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(Usuario usuario)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey.Trim()) < 32)
        {
            throw new InvalidOperationException(
                "La clave secreta JWT ('JwtSettings:SecretKey') no está configurada o es inválida. Debe poseer al menos 32 bytes UTF-8.");
        }

        var normalizedSecretKey = secretKey.Trim();
        var issuer = _configuration["JwtSettings:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            issuer = "VeterinariaPatitasAPI";
        }

        var audience = _configuration["JwtSettings:Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            audience = "VeterinariaPatitasClients";
        }

        var expirationMinutesStr = _configuration["JwtSettings:ExpirationMinutes"] ?? "480";

        if (!int.TryParse(expirationMinutesStr, out int expirationMinutes))
        {
            expirationMinutes = 480;
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(normalizedSecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Username),
            new(ClaimTypes.Email, usuario.Email),
            new(ClaimTypes.Role, usuario.Rol),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
