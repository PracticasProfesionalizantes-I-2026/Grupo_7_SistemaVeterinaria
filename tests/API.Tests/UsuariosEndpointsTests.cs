using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess.Context;
using DataAccess.Entities;
using DataAccess.Seeding;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.Usuario;
using Shared.Security;
using Xunit;

namespace API.Tests;

public class UsuariosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UsuariosEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObetenerTokenAsync(string username, string password)
    {
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = password
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(authData);
        Assert.NotNull(authData.Token);
        return authData.Token;
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_Returns200OK()
    {
        // Arrange (CU-12 Flujo principal)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);

        // Verificar que no se exponga ningún hash de contraseña
        var adminUser = users.FirstOrDefault(u => u.Username == "admin");
        Assert.NotNull(adminUser);
        Assert.Equal("Administrador", adminUser.Rol);
        Assert.Equal("Activo", adminUser.Estado);

        var blockedUser = users.FirstOrDefault(u => u.Username == "user.bloqueado");
        Assert.NotNull(blockedUser);
        Assert.Equal("Bloqueado", blockedUser.Estado);
        Assert.True(blockedUser.PuedeDesbloquear);

        var inactiveUser = users.FirstOrDefault(u => u.Username == "user.inactivo");
        Assert.NotNull(inactiveUser);
        Assert.Equal("Inactivo", inactiveUser.Estado);
        Assert.False(inactiveUser.PuedeDesbloquear);
    }

    [Fact]
    public async Task GetUsers_WithNonMatchingFilter_Returns200OKEmptyList()
    {
        // Arrange (CU-12 Flujo A1)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=usuario_que_no_existe_xyz");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    [Fact]
    public async Task GetUsers_WithNonAdminRole_Returns403Forbidden()
    {
        // Arrange (CU-12 Flujo A2: Veterinario no tiene acceso a gestión de usuarios)
        var vetToken = await ObetenerTokenAsync("vet.carlos", "Vet1234!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", vetToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutToken_Returns401Unauthorized()
    {
        // Arrange (CU-12 Flujo A2: Sin token)
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithAdminRole_Returns200OK()
    {
        // Arrange (CU-12 Paso 5: Detalle de usuario)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        // Primero obtenemos la lista para conocer un ID real
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=vet.carlos");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listRes = await _client.SendAsync(listReq);
        var users = await listRes.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        var targetUser = Assert.Single(users);

        // Act: Consultar por ID
        var detailReq = new HttpRequestMessage(HttpMethod.Get, $"/api/usuarios/{targetUser.Id}");
        detailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var detailRes = await _client.SendAsync(detailReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
        var detail = await detailRes.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        Assert.NotNull(detail);
        Assert.Equal(targetUser.Id, detail.Id);
        Assert.Equal("vet.carlos", detail.Username);
        Assert.Equal("Veterinario", detail.Rol);
        Assert.Equal("Activo", detail.Estado);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_Returns404NotFound()
    {
        // Arrange
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");
        var nonExistentId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/usuarios/{nonExistentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_FilterByRol_ReturnsOnlyMatchingRole()
    {
        // Arrange
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?rol=Recepcionista");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.Equal("Recepcionista", u.Rol));
    }

    [Fact]
    public async Task GetUsers_FilterByEstado_ReturnsOnlyMatchingEstado()
    {
        // Arrange
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?estado=Bloqueado");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var users = await response.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.Equal("Bloqueado", u.Estado));
    }

    [Fact]
    public async Task UnlockAccount_WithLockedUser_Returns200OK()
    {
        // Arrange (CU-13 Flujo principal)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        // Obtenemos el usuario bloqueado
        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=user.bloqueado");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listRes = await _client.SendAsync(listReq);
        var users = await listRes.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        var blockedUser = Assert.Single(users);

        // Act: Enviar POST /api/usuarios/{id}/desbloquear
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{blockedUser.Id}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.OK, unlockRes.StatusCode);
        var result = await unlockRes.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        Assert.NotNull(result);
        Assert.Equal(blockedUser.Id, result.Id);
        Assert.Equal("Activo", result.Estado);
        Assert.False(result.Bloqueado);
        Assert.Equal(0, result.IntentosFallidos);
        Assert.False(result.PuedeDesbloquear);
        Assert.True(result.PuedeRestablecerPassword);
    }

    [Fact]
    public async Task UnlockAccount_WithActiveUser_Returns400BadRequest()
    {
        // Arrange (CU-13 Flujo A1: Cuenta activa/no bloqueada)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=vet.carlos");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listRes = await _client.SendAsync(listReq);
        var users = await listRes.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        var activeUser = Assert.Single(users);

        // Act
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{activeUser.Id}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, unlockRes.StatusCode);
    }

    [Fact]
    public async Task UnlockAccount_WithInactiveUser_Returns400BadRequest()
    {
        // Arrange (Condición: Cuenta inactiva no debe habilitarse con desbloqueo)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=user.inactivo");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listRes = await _client.SendAsync(listReq);
        var users = await listRes.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        var inactiveUser = Assert.Single(users);

        // Act
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{inactiveUser.Id}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, unlockRes.StatusCode);
    }

    [Fact]
    public async Task UnlockAccount_WithNonAdminRole_Returns403Forbidden()
    {
        // Arrange (CU-13 Flujo A4: Rol insuficiente)
        var vetToken = await ObetenerTokenAsync("vet.carlos", "Vet1234!");
        var dummyId = Guid.NewGuid();

        // Act
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{dummyId}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", vetToken);
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, unlockRes.StatusCode);
    }

    [Fact]
    public async Task UnlockAccount_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var dummyId = Guid.NewGuid();

        // Act
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{dummyId}/desbloquear");
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, unlockRes.StatusCode);
    }

    [Fact]
    public async Task UnlockAccount_WithNonExistentUser_Returns404NotFound()
    {
        // Arrange
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");
        var nonExistentId = Guid.NewGuid();

        // Act
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{nonExistentId}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockRes = await _client.SendAsync(unlockReq);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, unlockRes.StatusCode);
    }

    [Fact]
    public async Task UnlockAccount_VerifiesPasswordRemainsUnchangedAndCanLogin()
    {
        // Arrange: Unlocks user and verifies login succeeds with original password
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/usuarios?username=user.bloqueado");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var listRes = await _client.SendAsync(listReq);
        var users = await listRes.Content.ReadFromJsonAsync<List<UsuarioResponseDTO>>();
        Assert.NotNull(users);
        var blockedUser = Assert.Single(users);

        // Desbloquear si está bloqueado
        if (blockedUser.Bloqueado)
        {
            var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{blockedUser.Id}/desbloquear");
            unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var unlockRes = await _client.SendAsync(unlockReq);
            Assert.Equal(HttpStatusCode.OK, unlockRes.StatusCode);
        }

        // Act: Intentar login con la contraseña original de user.bloqueado
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = "user.bloqueado",
            Password = "User1234!"
        };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginReq);

        // Assert: Login exitoso, probando que PasswordHash no fue alterado
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginData);
        Assert.False(string.IsNullOrWhiteSpace(loginData.Token));
        Assert.Equal("user.bloqueado", loginData.Username);
    }

    #region CU-14: Restablecer Contraseña

    [Fact]
    public async Task ResetPassword_WithValidUser_Returns200OK_AndEnforcesForcedChangeUponLogin()
    {
        // Arrange (CU-14 Flujo principal e integración con CU-01 A8)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var targetUserId = Guid.NewGuid();
        var targetUsername = "user.reset." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = targetUserId,
                Username = targetUsername,
                Email = $"{targetUsername}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Initial1234!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        // Act: Administrador restablece la contraseña
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{targetUserId}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await _client.SendAsync(resetReq);

        // Assert: 200 OK con contraseña temporal generada
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);
        var resetData = await resetRes.Content.ReadFromJsonAsync<RestablecerPasswordResponseDTO>();
        Assert.NotNull(resetData);
        Assert.Equal(targetUserId, resetData.UsuarioId);
        Assert.Equal(targetUsername, resetData.Username);
        Assert.False(string.IsNullOrWhiteSpace(resetData.PasswordTemporal));
        Assert.True(resetData.RequiereCambioPassword);
        Assert.False(resetData.Bloqueado);

        // Verificar flujo CU-01 A8: Intentar login con la contraseña temporal
        var loginTemporalReq = new LoginRequestDTO
        {
            UsuarioOEmail = targetUsername,
            Password = resetData.PasswordTemporal
        };
        var loginTemporalRes = await _client.PostAsJsonAsync("/api/auth/login", loginTemporalReq);
        Assert.Equal(HttpStatusCode.OK, loginTemporalRes.StatusCode);

        var loginTemporalData = await loginTemporalRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(loginTemporalData);
        Assert.True(loginTemporalData.RequiereCambioPassword);
        Assert.Null(loginTemporalData.Token); // No entrega token hasta cambiarla

        // Completar cambio obligatorio de contraseña
        var changeReq = new CambiarPasswordTemporalDTO
        {
            UsuarioOEmail = targetUsername,
            PasswordActual = resetData.PasswordTemporal,
            NuevaPassword = "NuevaPasswordSegura2026!",
            ConfirmacionPassword = "NuevaPasswordSegura2026!"
        };
        var changeRes = await _client.PostAsJsonAsync("/api/auth/cambiar-password-temporal", changeReq);
        Assert.Equal(HttpStatusCode.OK, changeRes.StatusCode);

        var changeData = await changeRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(changeData);
        Assert.Null(changeData.Token);
        Assert.False(changeData.RequiereCambioPassword);

        // Verificar que ahora puede autenticarse normalmente con la nueva contraseña y recibir su token JWT
        var normalLoginReq = new LoginRequestDTO
        {
            UsuarioOEmail = targetUsername,
            Password = "NuevaPasswordSegura2026!"
        };
        var normalLoginRes = await _client.PostAsJsonAsync("/api/auth/login", normalLoginReq);
        Assert.Equal(HttpStatusCode.OK, normalLoginRes.StatusCode);
        var normalLoginData = await normalLoginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(normalLoginData);
        Assert.False(normalLoginData.RequiereCambioPassword);
        Assert.False(string.IsNullOrWhiteSpace(normalLoginData.Token));
    }

    [Fact]
    public async Task ResetPassword_WithLockedUser_Returns200OK_AccountRemainsLocked()
    {
        // Arrange (CU-14 Flujo alternativo A4: Usuario bloqueado)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var lockedUserId = Guid.NewGuid();
        var lockedUsername = "user.locked." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = lockedUserId,
                Username = lockedUsername,
                Email = $"{lockedUsername}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Locked1234!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = true,
                IntentosFallidos = 5,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        // Act: Restablecer contraseña a usuario bloqueado
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{lockedUserId}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await _client.SendAsync(resetReq);

        // Assert: 200 OK informando restablecimiento y que la cuenta continúa bloqueada
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);
        var resetData = await resetRes.Content.ReadFromJsonAsync<RestablecerPasswordResponseDTO>();
        Assert.NotNull(resetData);
        Assert.True(resetData.Bloqueado);
        Assert.True(resetData.RequiereCambioPassword);
        Assert.Contains("permanece bloqueada", resetData.Mensaje);

        // Verificar que en la base de datos la cuenta sigue bloqueada
        var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/usuarios/{lockedUserId}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var getRes = await _client.SendAsync(getReq);
        var userCheck = await getRes.Content.ReadFromJsonAsync<UsuarioResponseDTO>();
        Assert.NotNull(userCheck);
        Assert.True(userCheck.Bloqueado);
        Assert.Equal("Bloqueado", userCheck.Estado);

        // Verificar que el usuario no puede iniciar sesión (CU-01 A3: 403 Forbidden por usuario bloqueado)
        var loginAttempt = new LoginRequestDTO
        {
            UsuarioOEmail = lockedUsername,
            Password = resetData.PasswordTemporal
        };
        var loginRes = await _client.PostAsJsonAsync("/api/auth/login", loginAttempt);
        Assert.Equal(HttpStatusCode.Forbidden, loginRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithInactiveUser_Returns400BadRequest()
    {
        // Arrange (Opción A: Cuenta inactiva no admite restablecimiento)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var inactiveUserId = Guid.NewGuid();
        var inactiveUsername = "user.inact." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = inactiveUserId,
                Username = inactiveUsername,
                Email = $"{inactiveUsername}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Inactive1234!"),
                Rol = "Veterinario",
                Activo = false,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        // Act
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{inactiveUserId}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await _client.SendAsync(resetReq);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, resetRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithNonAdminRole_Returns403Forbidden()
    {
        // Arrange (CU-14 Flujo alternativo A3)
        var vetToken = await ObetenerTokenAsync("vet.carlos", "Vet1234!");

        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{Guid.NewGuid()}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", vetToken);

        // Act
        var resetRes = await _client.SendAsync(resetReq);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, resetRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithoutToken_Returns401Unauthorized()
    {
        // Arrange
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{Guid.NewGuid()}/restablecer-password");

        // Act
        var resetRes = await _client.SendAsync(resetReq);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, resetRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithNonExistentUser_Returns404NotFound()
    {
        // Arrange
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");
        var nonExistentId = Guid.NewGuid();

        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{nonExistentId}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Act
        var resetRes = await _client.SendAsync(resetReq);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, resetRes.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_InvalidatesPreviouslyIssuedJwtToken_Returning401Unauthorized()
    {
        // Arrange (SEC-06: un token emitido antes de restablecer la contraseña queda invalidado cuando RequiereCambioPassword pasa a true)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");

        var targetUserId = Guid.NewGuid();
        var targetUsername = "user.sec06.reset." + Guid.NewGuid().ToString("N")[..8];
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Usuarios.Add(new Usuario
            {
                Id = targetUserId,
                Username = targetUsername,
                Email = $"{targetUsername}@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("InitialPass123!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false
            });
            await db.SaveChangesAsync();
        }

        // El usuario inicia sesión y obtiene un token JWT activo
        var userToken = await ObetenerTokenAsync(targetUsername, "InitialPass123!");

        var verifyBefore = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyBefore.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var verifyBeforeRes = await _client.SendAsync(verifyBefore);
        Assert.Equal(HttpStatusCode.OK, verifyBeforeRes.StatusCode);

        // Act: El Administrador restablece la contraseña del usuario (RequiereCambioPassword = true)
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{targetUserId}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await _client.SendAsync(resetReq);
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);

        // Assert: El token anterior del usuario ahora es rechazado con 401 Unauthorized en endpoints protegidos
        var verifyAfterReset = new HttpRequestMessage(HttpMethod.Get, "/api/auth/verificar-sesion");
        verifyAfterReset.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var verifyAfterResetRes = await _client.SendAsync(verifyAfterReset);
        Assert.Equal(HttpStatusCode.Unauthorized, verifyAfterResetRes.StatusCode);

        var duenosAfterReset = new HttpRequestMessage(HttpMethod.Get, "/api/duenos");
        duenosAfterReset.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        var duenosAfterResetRes = await _client.SendAsync(duenosAfterReset);
        Assert.Equal(HttpStatusCode.Unauthorized, duenosAfterResetRes.StatusCode);
    }

    [Fact]
    public async Task UnhandledInvalidOperationException_InUsuariosEndpoints_Returns500GenericJsonWithoutLeakingInternalDetails()
    {
        // Arrange (ERR-01 y ERR-03: Un InvalidOperationException inesperado en capa de servicio/persistencia debe retornar 500 genérico y NO 400 ni filtrar ex.Message)
        const string internalDbSecretMessage = "SQLITE_CORRUPT: Falla interna crítica en tabla Usuarios (detalle sensible)";

        using var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.AddScoped<BusinessLogic.Interfaces.IUsuarioService>(_ => new FailingUsuarioServiceStub(internalDbSecretMessage));
            });
        });

        using var client = customFactory.CreateClient();
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = "admin",
            Password = "Admin1234!"
        });
        loginRes.EnsureSuccessStatusCode();
        var authData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        var adminToken = authData!.Token!;

        // Act 1: Desbloquear cuando ocurre una falla interna (InvalidOperationException)
        var unlockReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{Guid.NewGuid()}/desbloquear");
        unlockReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var unlockRes = await client.SendAsync(unlockReq);

        // Assert 1: 500 Internal Server Error con mensaje genérico y sin exponer detalles técnicos
        Assert.Equal(HttpStatusCode.InternalServerError, unlockRes.StatusCode);
        var unlockBody = await unlockRes.Content.ReadAsStringAsync();
        Assert.Contains("Ocurrió un error interno al procesar la solicitud.", unlockBody);
        Assert.DoesNotContain(internalDbSecretMessage, unlockBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", unlockBody, StringComparison.OrdinalIgnoreCase);

        // Act 2: RestablecerPassword cuando ocurre una falla interna (InvalidOperationException)
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/usuarios/{Guid.NewGuid()}/restablecer-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var resetRes = await client.SendAsync(resetReq);

        // Assert 2: 500 Internal Server Error con mensaje genérico y sin exponer detalles técnicos
        Assert.Equal(HttpStatusCode.InternalServerError, resetRes.StatusCode);
        var resetBody = await resetRes.Content.ReadAsStringAsync();
        Assert.Contains("Ocurrió un error interno al procesar la solicitud.", resetBody);
        Assert.DoesNotContain(internalDbSecretMessage, resetBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StackTrace", resetBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DomainErrorResponses_DoNotExposeInternalGuidOrRuleCodesInHttpResponse()
    {
        // Arrange (ERR-02: Las respuestas HTTP 404/409 no deben exponer el Guid buscado ni códigos RN-XX)
        var adminToken = await ObetenerTokenAsync("admin", "Admin1234!");
        var nonExistentId = Guid.NewGuid();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/usuarios/{nonExistentId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(nonExistentId.ToString(), content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No se encontró el usuario solicitado.", content);
    }

    private sealed class FailingUsuarioServiceStub : BusinessLogic.Interfaces.IUsuarioService
    {
        private readonly string _internalMessage;

        public FailingUsuarioServiceStub(string internalMessage)
        {
            _internalMessage = internalMessage;
        }

        public Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync(string? username = null, string? rol = null, string? estado = null)
            => throw new InvalidOperationException(_internalMessage);

        public Task<UsuarioResponseDTO> GetByIdAsync(Guid id)
            => throw new InvalidOperationException(_internalMessage);

        public Task<UsuarioResponseDTO> DesbloquearCuentaAsync(Guid usuarioId, Guid administradorId, string administradorUsername)
            => throw new InvalidOperationException(_internalMessage);

        public Task<RestablecerPasswordResponseDTO> RestablecerPasswordAsync(Guid usuarioId, Guid administradorId, string administradorUsername)
            => throw new InvalidOperationException(_internalMessage);
    }

    #endregion
}
