using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.Auth;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly Mock<IPasswordHasherService> _hasherMock;
    private readonly Mock<IJwtTokenGenerator> _jwtMock;
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _hasherMock = new Mock<IPasswordHasherService>();
        _jwtMock = new Mock<IJwtTokenGenerator>();
        _service = new AuthService(_usuarioRepoMock.Object, _hasherMock.Object, _jwtMock.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccessAndResetsFailedAttempts()
    {
        // Arrange (Flujo principal)
        var dto = new LoginRequestDTO { UsuarioOEmail = "vet.carlos", Password = "Password123!" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "vet.carlos",
            Email = "carlos@patitas.com",
            PasswordHash = "hash123",
            Rol = "Veterinario",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 2,
            RequiereCambioPassword = false
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("vet.carlos"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("Password123!", "hash123"))
                   .Returns(true);
        _jwtMock.Setup(j => j.GenerateToken(usuario))
                .Returns("token-jwt-valido");

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("token-jwt-valido", result.Token);
        Assert.Equal("vet.carlos", result.Username);
        Assert.Equal("Veterinario", result.Rol);
        Assert.False(result.RequiereCambioPassword);
        Assert.Equal(0, usuario.IntentosFallidos);
        _usuarioRepoMock.Verify(r => r.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithWrongPassword_IncrementsFailedAttempts()
    {
        // Arrange (A1)
        var dto = new LoginRequestDTO { UsuarioOEmail = "recep.ana", Password = "WrongPassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "recep.ana",
            Email = "ana@patitas.com",
            PasswordHash = "hash123",
            Rol = "Recepcionista",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 1
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("recep.ana"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPassword", "hash123"))
                   .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _service.LoginAsync(dto));
        Assert.Equal(2, usuario.IntentosFallidos);
        Assert.False(usuario.Bloqueado);
        _usuarioRepoMock.Verify(r => r.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_OnFifthFailedAttempt_LocksUserAccountAndThrowsUsuarioBloqueadoException()
    {
        // Arrange (A2)
        var dto = new LoginRequestDTO { UsuarioOEmail = "recep.ana", Password = "WrongPassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "recep.ana",
            Email = "ana@patitas.com",
            PasswordHash = "hash123",
            Rol = "Recepcionista",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 4
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("recep.ana"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("WrongPassword", "hash123"))
                   .Returns(false);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UsuarioBloqueadoException>(() => _service.LoginAsync(dto));
        Assert.Equal("Usuario bloqueado. Contacte al Administrador.", ex.Message);
        Assert.Equal(5, usuario.IntentosFallidos);
        Assert.True(usuario.Bloqueado);
        _usuarioRepoMock.Verify(r => r.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithLockedAccount_ThrowsUsuarioBloqueadoException()
    {
        // Arrange (A3)
        var dto = new LoginRequestDTO { UsuarioOEmail = "user.bloqueado", Password = "AnyPassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "user.bloqueado",
            Email = "bloqueado@patitas.com",
            PasswordHash = "hash123",
            Rol = "Recepcionista",
            Activo = true,
            Bloqueado = true,
            IntentosFallidos = 5
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("user.bloqueado"))
                        .ReturnsAsync(usuario);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UsuarioBloqueadoException>(() => _service.LoginAsync(dto));
        Assert.Equal("Usuario bloqueado. Contacte al Administrador.", ex.Message);
        _hasherMock.Verify(h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentialsAfterPreviousFailures_ResetsCounterToZero()
    {
        // Arrange (A4)
        var dto = new LoginRequestDTO { UsuarioOEmail = "admin", Password = "CorrectPassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "admin",
            Email = "admin@patitas.com",
            PasswordHash = "hash123",
            Rol = "Administrador",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 3
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("admin"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("CorrectPassword", "hash123"))
                   .Returns(true);
        _jwtMock.Setup(j => j.GenerateToken(usuario)).Returns("admin-token");

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        Assert.Equal(0, usuario.IntentosFallidos);
        Assert.Equal("admin-token", result.Token);
        _usuarioRepoMock.Verify(r => r.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInactiveUser_ThrowsUsuarioInactivoException()
    {
        // Arrange (A6)
        var dto = new LoginRequestDTO { UsuarioOEmail = "user.inactivo", Password = "SomePassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "user.inactivo",
            Email = "inactivo@patitas.com",
            PasswordHash = "hash123",
            Rol = "Veterinario",
            Activo = false,
            Bloqueado = false
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("user.inactivo"))
                        .ReturnsAsync(usuario);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UsuarioInactivoException>(() => _service.LoginAsync(dto));
        Assert.Equal("El usuario no se encuentra habilitado para operar en el sistema.", ex.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WithNonExistentUser_ThrowsCredencialesInvalidasException()
    {
        // Arrange
        var dto = new LoginRequestDTO { UsuarioOEmail = "inexistente", Password = "SomePassword" };
        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("inexistente"))
                        .ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _service.LoginAsync(dto));
    }

    [Fact]
    public async Task AuthenticateAsync_WithTemporaryPassword_ReturnsRequiresPasswordChangeFlagWithoutToken()
    {
        // Arrange (A8)
        var dto = new LoginRequestDTO { UsuarioOEmail = "user.temporal", Password = "TemporalPassword" };
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "user.temporal",
            Email = "temporal@patitas.com",
            PasswordHash = "hash123",
            Rol = "Recepcionista",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 0,
            RequiereCambioPassword = true
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("user.temporal"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("TemporalPassword", "hash123"))
                   .Returns(true);

        // Act
        var result = await _service.LoginAsync(dto);

        // Assert
        Assert.True(result.RequiereCambioPassword);
        Assert.Null(result.Token);
        _jwtMock.Verify(j => j.GenerateToken(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPasswordTemporalAsync_WithValidData_UpdatesPasswordAndClearsFlag()
    {
        // Arrange (A8 cambio exitoso)
        var dto = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "user.temporal",
            PasswordActual = "TemporalPassword",
            NuevaPassword = "NuevaPassword123!",
            ConfirmacionPassword = "NuevaPassword123!"
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "user.temporal",
            Email = "temporal@patitas.com",
            PasswordHash = "hash-viejo",
            Rol = "Recepcionista",
            Activo = true,
            Bloqueado = false,
            RequiereCambioPassword = true
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("user.temporal"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("TemporalPassword", "hash-viejo"))
                   .Returns(true);
        _hasherMock.Setup(h => h.HashPassword("NuevaPassword123!"))
                   .Returns("hash-nuevo");
        _jwtMock.Setup(j => j.GenerateToken(usuario))
                .Returns("token-post-cambio");

        // Act
        var result = await _service.CambiarPasswordTemporalAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.RequiereCambioPassword);
        Assert.Equal("token-post-cambio", result.Token);
        Assert.False(usuario.RequiereCambioPassword);
        Assert.Equal("hash-nuevo", usuario.PasswordHash);
        _usuarioRepoMock.Verify(r => r.UpdateAsync(usuario), Times.Once);
    }

    [Fact]
    public async Task CambiarPasswordTemporalAsync_WithMismatchedConfirmation_ThrowsValidationException()
    {
        // Arrange
        var dto = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "user.temporal",
            PasswordActual = "TemporalPassword",
            NuevaPassword = "NuevaPassword123!",
            ConfirmacionPassword = "OtraPasswordDiferente"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CambiarPasswordTemporalAsync(dto));
        _usuarioRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
    }

    [Fact]
    public async Task CambiarPasswordTemporalAsync_WithSimplePassword_ThrowsValidationException()
    {
        // Arrange (menos de 8 caracteres o sin mayúsculas/números)
        var dto = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "user.temporal",
            PasswordActual = "TemporalPassword",
            NuevaPassword = "simple",
            ConfirmacionPassword = "simple"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CambiarPasswordTemporalAsync(dto));
    }

    [Fact]
    public async Task CambiarPasswordTemporalAsync_WithWrongCurrentPassword_ThrowsCredencialesInvalidasException()
    {
        // Arrange
        var dto = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "user.temporal",
            PasswordActual = "PasswordIncorrecta",
            NuevaPassword = "NuevaPassword123!",
            ConfirmacionPassword = "NuevaPassword123!"
        };

        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "user.temporal",
            Email = "temporal@patitas.com",
            PasswordHash = "hash-real",
            Activo = true,
            Bloqueado = false,
            RequiereCambioPassword = true
        };

        _usuarioRepoMock.Setup(r => r.GetByUsernameOrEmailTrackedAsync("user.temporal"))
                        .ReturnsAsync(usuario);
        _hasherMock.Setup(h => h.VerifyPassword("PasswordIncorrecta", "hash-real"))
                   .Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<CredencialesInvalidasException>(() => _service.CambiarPasswordTemporalAsync(dto));
        _usuarioRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Usuario>()), Times.Never);
    }
}
