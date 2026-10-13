using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Auth;
using Shared.DTOs.Usuario;
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
}
