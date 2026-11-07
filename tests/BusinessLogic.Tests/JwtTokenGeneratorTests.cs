using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusinessLogic.Services;
using DataAccess.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace BusinessLogic.Tests;

public class JwtTokenGeneratorTests
{
    private const string ValidTestSecretKey = "unit-test-secret-key-with-at-least-32-utf8-bytes-9876543210!";

    private static Usuario CrearUsuarioPrueba()
    {
        return new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "vet.carlos",
            Email = "carlos@patitas.com",
            Rol = "Veterinario",
            Activo = true
        };
    }

    private static IConfiguration CrearConfiguracionMock(
        string? secretKey,
        string? issuer = "VeterinariaPatitasAPI",
        string? audience = "VeterinariaPatitasClients",
        string? expirationMinutes = "120")
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["JwtSettings:SecretKey"]).Returns(secretKey);
        mockConfig.Setup(c => c["JwtSettings:Issuer"]).Returns(issuer);
        mockConfig.Setup(c => c["JwtSettings:Audience"]).Returns(audience);
        mockConfig.Setup(c => c["JwtSettings:ExpirationMinutes"]).Returns(expirationMinutes);
        return mockConfig.Object;
    }

    [Fact]
    public void GenerateToken_WithValidSecretKey_GeneratesValidJwtWithExpectedClaims()
    {
        var config = CrearConfiguracionMock(ValidTestSecretKey);
        var generator = new JwtTokenGenerator(config);
        var usuario = CrearUsuarioPrueba();

        var tokenString = generator.GenerateToken(usuario);

        Assert.False(string.IsNullOrWhiteSpace(tokenString));

        var handler = new JwtSecurityTokenHandler();
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "VeterinariaPatitasAPI",
            ValidateAudience = true,
            ValidAudience = "VeterinariaPatitasClients",
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ValidTestSecretKey))
        };

        var principal = handler.ValidateToken(tokenString, validationParameters, out var validatedToken);
        Assert.NotNull(validatedToken);
        Assert.Equal(usuario.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(usuario.Username, principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.Equal(usuario.Email, principal.FindFirst(ClaimTypes.Email)?.Value);
        Assert.Equal(usuario.Rol, principal.FindFirst(ClaimTypes.Role)?.Value);
        Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("clave-corta-menor-32-bytes")]
    [InlineData("1234567890123456789012345678901")] // 31 bytes UTF-8
    public void GenerateToken_WhenSecretKeyIsMissingOrShorterThan32Bytes_ThrowsInvalidOperationException(string? invalidKey)
    {
        var config = CrearConfiguracionMock(invalidKey);
        var generator = new JwtTokenGenerator(config);
        var usuario = CrearUsuarioPrueba();

        var ex = Assert.Throws<InvalidOperationException>(() => generator.GenerateToken(usuario));
        Assert.Contains("JwtSettings:SecretKey", ex.Message);
    }
}
