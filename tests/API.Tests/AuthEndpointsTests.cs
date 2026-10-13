using System.Net;
using System.Net.Http.Json;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.Security;
using Xunit;

namespace API.Tests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidUsernameCredentials_Returns200OK_WithJwtToken()
    {
        // Flujo principal: login con username
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "admin",
            Password = "Admin1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("admin", result.Username);
        Assert.Equal("Administrador", result.Rol);
        Assert.False(result.RequiereCambioPassword);
    }

    [Fact]
    public async Task Login_WithValidEmailCredentials_Returns200OK_WithJwtToken()
    {
        // Flujo principal: login con email
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "carlos.veterinario@patitas.com",
            Password = "Vet1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
        Assert.Equal("vet.carlos", result.Username);
        Assert.Equal("Veterinario", result.Rol);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_Returns401Unauthorized()
    {
        // Flujo alternativo A1
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "recep.ana",
            Password = "PasswordIncorrecta"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_OnFifthFailedAttempt_Returns403Forbidden_UserLocked()
    {
        // Flujo alternativo A2: cuenta que acumula 4 intentos y en el 5to se bloquea
        var username = "user.test.bloqueo." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("TestPass123!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 4
            });
            await db.SaveChangesAsync();
        }

        var request = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "WrongPassword"
        };

        // 5to intento fallido consecutivo
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verificamos que la cuenta haya quedado efectivamente bloqueada en BD
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var usuario = db.Usuarios.First(u => u.Username == username);
            Assert.True(usuario.Bloqueado);
            Assert.Equal(5, usuario.IntentosFallidos);
        }
    }

    [Fact]
    public async Task Login_WithLockedAccount_Returns403Forbidden()
    {
        // Flujo alternativo A3: cuenta previamente bloqueada
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "user.bloqueado",
            Password = "CualquierPassword"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithInactiveAccount_Returns403Forbidden()
    {
        // Flujo alternativo A6: cuenta deshabilitada/inactiva
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "user.inactivo",
            Password = "User1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_Returns400BadRequest()
    {
        // Flujo alternativo A5: campos obligatorios vacíos
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "",
            Password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithTemporaryPassword_Returns200OK_WithRequiresChangeFlagAndNoToken()
    {
        // Flujo alternativo A8: contraseña temporal detectada
        var request = new LoginRequestDTO
        {
            UsuarioOEmail = "user.temporal",
            Password = "Temporal1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(result);
        Assert.True(result.RequiereCambioPassword);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WithValidData_Returns200OK_AndEnablesNormalLogin()
    {
        // Flujo alternativo A8: cambio obligatorio de contraseña temporal
        var username = "user.temp." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("TempPass123!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = true
            });
            await db.SaveChangesAsync();
        }

        var cambioRequest = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "TempPass123!",
            NuevaPassword = "NuevaPasswordDefinitiva123!",
            ConfirmacionPassword = "NuevaPasswordDefinitiva123!"
        };

        var cambioResponse = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", cambioRequest);

        Assert.Equal(HttpStatusCode.OK, cambioResponse.StatusCode);
        var cambioResult = await cambioResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(cambioResult);
        Assert.False(cambioResult.RequiereCambioPassword);
        Assert.False(string.IsNullOrWhiteSpace(cambioResult.Token));

        // Comprobamos que el siguiente login funcione con la nueva clave y entregue token normal
        var nuevoLogin = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "NuevaPasswordDefinitiva123!"
        };

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", nuevoLogin);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginResult);
        Assert.False(loginResult.RequiereCambioPassword);
        Assert.False(string.IsNullOrWhiteSpace(loginResult.Token));
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WithMismatchedPasswords_Returns400BadRequest()
    {
        var cambioRequest = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "admin",
            PasswordActual = "Admin1234!",
            NuevaPassword = "NuevaPassword123!",
            ConfirmacionPassword = "PasswordDiferente123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", cambioRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WithSimplePassword_Returns400BadRequest()
    {
        var cambioRequest = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "admin",
            PasswordActual = "Admin1234!",
            NuevaPassword = "simple",
            ConfirmacionPassword = "simple"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", cambioRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithValidBearerToken_Returns200OK()
    {
        // Arrange: login para obtener token válido (CU-09 Flujo principal)
        var loginReq = new LoginRequestDTO { UsuarioOEmail = "admin", Password = "Admin1234!" };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        // Act: enviar POST /api/auth/logout con Authorization Bearer
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var logoutRes = await _client.SendAsync(logoutReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutAuthorizationHeader_Returns401Unauthorized()
    {
        // Act: enviar POST /api/auth/logout sin cabecera de autorización (CU-09 Flujo 1a)
        var logoutRes = await _client.PostAsync("/api/auth/logout", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, logoutRes.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenTokenAlreadyRevoked_Returns200OK()
    {
        // Arrange: login y primer logout (CU-09 Flujo 3a: idempotencia)
        var loginReq = new LoginRequestDTO { UsuarioOEmail = "vet.carlos", Password = "Vet1234!" };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        var firstLogoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        firstLogoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var firstLogoutRes = await _client.SendAsync(firstLogoutReq);
        Assert.Equal(HttpStatusCode.OK, firstLogoutRes.StatusCode);

        // Act: segundo intento de logout con el mismo token ya revocado
        var secondLogoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        secondLogoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var secondLogoutRes = await _client.SendAsync(secondLogoutReq);

        // Assert: completa exitosamente de forma idempotente con 200 OK
        Assert.Equal(HttpStatusCode.OK, secondLogoutRes.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterLogout_Returns401Unauthorized()
    {
        // Arrange: login para obtener token
        var loginReq = new LoginRequestDTO { UsuarioOEmail = "recep.ana", Password = "Recep1234!" };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        // Verificamos que el token funcione antes de logout
        var verifyReq1 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyReq1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var verifyRes1 = await _client.SendAsync(verifyReq1);
        Assert.Equal(HttpStatusCode.OK, verifyRes1.StatusCode);

        // Act: Cerramos sesión (logout)
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);

        // Assert: Intentar usar el token nuevamente es rechazado con 401 Unauthorized
        var verifyReq2 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyReq2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var verifyRes2 = await _client.SendAsync(verifyReq2);
        Assert.Equal(HttpStatusCode.Unauthorized, verifyRes2.StatusCode);
    }
}
