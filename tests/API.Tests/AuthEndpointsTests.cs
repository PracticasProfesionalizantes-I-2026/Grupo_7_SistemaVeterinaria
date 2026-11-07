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
    public async Task CambiarPasswordTemporal_WithValidData_Returns200OKWithoutToken_ResetsFailedAttempts_AndEnablesNormalLogin()
    {
        // Flujo alternativo A8: cambio obligatorio de contraseña temporal (Opción B: sin emitir JWT en el cambio)
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
                IntentosFallidos = 3,
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
        Assert.Null(cambioResult.Token);

        // Verificar que el contador de intentos fallidos se haya reiniciado a 0
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var userInDb = db.Usuarios.First(u => u.Username == username);
            Assert.Equal(0, userInDb.IntentosFallidos);
            Assert.False(userInDb.RequiereCambioPassword);
        }

        // Comprobamos que la contraseña temporal anterior ya no puede utilizarse
        var loginConPasswordVieja = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "TempPass123!"
        };
        var loginViejoResponse = await _client.PostAsJsonAsync("/api/auth/login", loginConPasswordVieja);
        Assert.Equal(HttpStatusCode.Unauthorized, loginViejoResponse.StatusCode);

        // Comprobamos que el login posterior funcione con la nueva contraseña definitiva y entregue token JWT
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
    public async Task CambiarPasswordTemporal_WhenReusingSameTemporaryPassword_Returns400BadRequest()
    {
        var username = "user.samepass." + Guid.NewGuid().ToString("N")[..8];
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
            NuevaPassword = "TempPass123!",
            ConfirmacionPassword = "TempPass123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", cambioRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WhenRequiereCambioPasswordIsFalse_Returns400BadRequest()
    {
        var username = "user.nochange." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Definitiva123!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        var cambioRequest = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "Definitiva123!",
            NuevaPassword = "OtraPasswordDef123!",
            ConfirmacionPassword = "OtraPasswordDef123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", cambioRequest);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WithWrongCurrentPassword_AndNonExistentUser_ReturnSameGeneric401Message()
    {
        var username = "user.wrongpass." + Guid.NewGuid().ToString("N")[..8];
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

        var wrongPassReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "ClaveIncorrecta999!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        };
        var nonExistentReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = "usuario.no.existe.9999",
            PasswordActual = "ClaveIncorrecta999!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        };

        var wrongPassRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", wrongPassReq);
        var nonExistentRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", nonExistentReq);

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassRes.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, nonExistentRes.StatusCode);

        var wrongPassBody = await wrongPassRes.Content.ReadAsStringAsync();
        var nonExistentBody = await nonExistentRes.Content.ReadAsStringAsync();
        Assert.Equal(wrongPassBody, nonExistentBody);
        Assert.Contains("Nombre de usuario o contraseña incorrectos.", wrongPassBody);

        // Verificar que incrementó IntentosFallidos en la base de datos
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var userInDb = db.Usuarios.First(u => u.Username == username);
            Assert.Equal(1, userInDb.IntentosFallidos);
        }
    }

    [Fact]
    public async Task CambiarPasswordTemporal_FiveConsecutiveFailedAttempts_LocksAccountOnFifthAttempt()
    {
        var username = "user.lock5temp." + Guid.NewGuid().ToString("N")[..8];
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

        var failedReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "Incorrecta123!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        };

        for (int i = 1; i <= 4; i++)
        {
            var res = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", failedReq);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        var fifthRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", failedReq);
        Assert.Equal(HttpStatusCode.Forbidden, fifthRes.StatusCode);

        // Incluso con la contraseña temporal correcta ahora debe rechazar por cuenta bloqueada
        var validReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "TempPass123!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        };
        var afterLockRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", validReq);
        Assert.Equal(HttpStatusCode.Forbidden, afterLockRes.StatusCode);
    }

    [Fact]
    public async Task CambiarPasswordTemporal_AlternatingFailedAttemptsWithLogin_SharesCounterAndLocksOnFifthAttempt()
    {
        var username = "user.altlock." + Guid.NewGuid().ToString("N")[..8];
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

        var badLoginReq = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "WrongLoginPass!"
        };
        var badCambioReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = username,
            PasswordActual = "WrongCambioPass!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        };

        // 3 intentos fallidos en /api/auth/login
        for (int i = 1; i <= 3; i++)
        {
            var res = await _client.PostAsJsonAsync("/api/auth/login", badLoginReq);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }

        // 4to intento fallido en /api/auth/cambiar-password-temporal
        var fourthRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", badCambioReq);
        Assert.Equal(HttpStatusCode.Unauthorized, fourthRes.StatusCode);

        // 5to intento fallido en /api/auth/cambiar-password-temporal -> Bloquea la cuenta (403 Forbidden)
        var fifthRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", badCambioReq);
        Assert.Equal(HttpStatusCode.Forbidden, fifthRes.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var userInDb = db.Usuarios.First(u => u.Username == username);
            Assert.True(userInDb.Bloqueado);
            Assert.Equal(5, userInDb.IntentosFallidos);
        }
    }

    [Fact]
    public async Task CambiarPasswordTemporal_WithInactiveOrLockedAccount_Returns403Forbidden()
    {
        var inactiveUser = "user.inact.temp." + Guid.NewGuid().ToString("N")[..8];
        var lockedUser = "user.lock.temp." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.AddRange(
                new Usuario
                {
                    Id = Guid.NewGuid(),
                    Username = inactiveUser,
                    Email = $"{inactiveUser}@patitas.com",
                    PasswordHash = PasswordHelper.HashPassword("TempPass123!"),
                    Rol = "Recepcionista",
                    Activo = false,
                    Bloqueado = false,
                    IntentosFallidos = 0,
                    RequiereCambioPassword = true
                },
                new Usuario
                {
                    Id = Guid.NewGuid(),
                    Username = lockedUser,
                    Email = $"{lockedUser}@patitas.com",
                    PasswordHash = PasswordHelper.HashPassword("TempPass123!"),
                    Rol = "Recepcionista",
                    Activo = true,
                    Bloqueado = true,
                    IntentosFallidos = 5,
                    RequiereCambioPassword = true
                });
            await db.SaveChangesAsync();
        }

        var inactiveRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = inactiveUser,
            PasswordActual = "TempPass123!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        });
        var lockedRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = lockedUser,
            PasswordActual = "TempPass123!",
            NuevaPassword = "NuevaPasswordDef123!",
            ConfirmacionPassword = "NuevaPasswordDef123!"
        });

        Assert.Equal(HttpStatusCode.Forbidden, inactiveRes.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, lockedRes.StatusCode);
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

    [Fact]
    public async Task ProtectedEndpoint_WithTamperedOrMalformedToken_Returns401Unauthorized()
    {
        var loginReq = new LoginRequestDTO { UsuarioOEmail = "vet.carlos", Password = "Vet1234!" };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        // Alterar la firma del token
        var parts = loginData.Token.Split('.');
        Assert.Equal(3, parts.Length);
        var tamperedToken = $"{parts[0]}.{parts[1]}.FirmaManipuladaInvalida12345";

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tamperedToken);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenSignedByDifferentKey_Returns401Unauthorized()
    {
        var differentKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
             System.Text.Encoding.UTF8.GetBytes("otra-clave-secreta-distinta-de-mas-de-32-bytes-utf8-9999!"));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            differentKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "VeterinariaPatitasAPI",
            audience: "VeterinariaPatitasClients",
            claims: new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "vet.carlos"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Veterinario"),
                new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("clave-demasiado-corta-123")]
    public void ApplicationStartup_WhenJwtSecretKeyIsMissingOrInvalid_ThrowsInvalidOperationException(string invalidSecretKey)
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            using var invalidFactory = _factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("JwtSettings:SecretKey", invalidSecretKey);
            });
            using var _ = invalidFactory.CreateClient();
        });

        Assert.Contains("JwtSettings:SecretKey", ex.Message);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterAccountBecomesLocked_Returns401Unauthorized_WhileLogoutRemainsIdempotent()
    {
        // 1. Crear un usuario activo y obtener su token JWT válido
        var username = "user.sec06.lock." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = $"{username}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("ValidPass123!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "ValidPass123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        // Comprobar que el token funciona mientras la cuenta está activa y desbloqueada
        var verifyBefore = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyBefore.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var verifyBeforeRes = await _client.SendAsync(verifyBefore);
        Assert.Equal(HttpStatusCode.OK, verifyBeforeRes.StatusCode);

        // 2. Bloquear la cuenta por 5 intentos fallidos consecutivos
        for (int i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
            {
                UsuarioOEmail = username,
                Password = "WrongPassword999!"
            });
        }

        // 3. Intentar usar el token emitido previamente en un endpoint protegido -> Debe ser rechazado con 401 Unauthorized
        var verifyAfterLock = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyAfterLock.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var verifyAfterLockRes = await _client.SendAsync(verifyAfterLock);
        Assert.Equal(HttpStatusCode.Unauthorized, verifyAfterLockRes.StatusCode);

        // 4. Verificar que el cierre de sesión (/api/auth/logout) se mantiene permitido e idempotente (200 OK)
        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutReq.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var logoutRes = await _client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_AfterUserIsDeactivated_Returns401Unauthorized()
    {
        var userId = Guid.NewGuid();
        var username = "user.sec06.inact." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = userId,
                Username = username,
                Email = $"{username}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("ValidPass123!"),
                Rol = "Veterinario",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = "ValidPass123!"
        });
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData?.Token);

        // Desactivar al usuario en la base de datos
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var userInDb = db.Usuarios.First(u => u.Id == userId);
            userInDb.Activo = false;
            await db.SaveChangesAsync();
        }

        // El token emitido previamente debe ser rechazado con 401 Unauthorized
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginData.Token);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithTokenOfNonExistentUser_Returns401Unauthorized()
    {
        // Generar un token firmado con la clave válida de pruebas pero con un NameIdentifier que no existe en la BD
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(CustomWebApplicationFactory.TestJwtSecretKey));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
            key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "VeterinariaPatitasAPI",
            audience: "VeterinariaPatitasClients",
            claims: new[]
            {
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, "usuario.fantasma"),
                new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, "Veterinario"),
                new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        var tokenString = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);

        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenString);
        var res = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }
}
