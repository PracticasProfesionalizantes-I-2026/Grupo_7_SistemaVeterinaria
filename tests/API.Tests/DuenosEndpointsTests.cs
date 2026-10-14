using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Auth;
using Shared.DTOs.Dueno;
using Xunit;

namespace API.Tests;

public class DuenosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DuenosEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string username, string password)
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
    public async Task GetAll_Retorna200OkYListaDeDuenos()
    {
        var response = await _client.GetAsync("/api/duenos");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var duenos = await response.Content.ReadFromJsonAsync<List<DuenoResponseDTO>>();
        Assert.NotNull(duenos);
        Assert.True(duenos.Count >= 2); // Datos sembrados por DbInitializer
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404NotFound()
    {
        var idInexistente = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/duenos/{idInexistente}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_DatosValidos_ConRolRecepcionista_Retorna201Created()
    {
        // Arrange (CU-02 Flujo principal con Recepcionista)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Federico",
            Apellido = "Romero",
            Dni = "42" + Random.Shared.Next(100000, 999999),
            Telefono = "11-3344-5566",
            Email = $"fede_{Guid.NewGuid():N}@test.com",
            Direccion = "Av. de Mayo 500"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var creado = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(nuevo.Nombre, creado.Nombre);
        Assert.Equal(nuevo.Dni, creado.Dni);
    }

    [Fact]
    public async Task Create_DatosValidos_ConRolVeterinario_Retorna201Created()
    {
        // Arrange (CU-02 Flujo principal con Veterinario)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Lucía",
            Apellido = "Méndez",
            Dni = "43" + Random.Shared.Next(100000, 999999),
            Telefono = "11-8899-0011",
            Email = $"lucia_{Guid.NewGuid():N}@test.com",
            Direccion = "Belgrano 123"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(nuevo.Nombre, creado.Nombre);
        Assert.Equal(nuevo.Dni, creado.Dni);
    }

    [Fact]
    public async Task Create_ConRolAdministrador_Retorna403Forbidden()
    {
        // Arrange (CU-02 Precondiciones: Administrador no autorizado)
        var adminToken = await ObtenerTokenAsync("admin", "Admin1234!");

        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Admin",
            Apellido = "Dueño",
            Dni = "44" + Random.Shared.Next(100000, 999999),
            Telefono = "11-1111-2222",
            Email = "admin.dueño@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        req.Content = JsonContent.Create(nuevo);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinToken_Retorna401Unauthorized()
    {
        // Arrange (CU-02 Petición anónima)
        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Sin",
            Apellido = "Token",
            Dni = "45112233",
            Telefono = "11-0000-0000",
            Email = "anonimo@test.com"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/duenos", nuevo);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_DniDuplicado_Retorna409Conflict()
    {
        // Arrange (RN-05: DNI ya registrado por DbInitializer)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var duplicado = new DuenoCreateDTO
        {
            Nombre = "Clon",
            Apellido = "González",
            Dni = "34567890", // DNI de Carlos González sembrado
            Telefono = "11-0000-0000",
            Email = "clon@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(duplicado);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_CamposObligatoriosFaltantes_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2a)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var incompleto = new DuenoCreateDTO
        {
            Nombre = "", // Requerido vacío
            Apellido = "Gómez",
            Dni = "30112233",
            Telefono = "",
            Email = "valido@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(incompleto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_DniNoNumerico_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2b)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "María",
            Apellido = "López",
            Dni = "30A12345", // Letra en DNI
            Telefono = "11-4455-6677",
            Email = "maria@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmailInvalido_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2b)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "María",
            Apellido = "López",
            Dni = "38112233",
            Telefono = "11-4455-6677",
            Email = "email-sin-arroba" // Formato inválido
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_CampoConSoloEspacios_Retorna400BadRequest()
    {
        // Arrange (CU-02 Flujo 2c)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dto = new DuenoCreateDTO
        {
            Nombre = "   ", // Solo espacios
            Apellido = "López",
            Dni = "38112233",
            Telefono = "11-4455-6677",
            Email = "maria@test.com"
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/duenos");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(dto);

        // Act
        var response = await _client.SendAsync(req);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_DuenoConMascotas_Retorna409Conflict()
    {
        // Obtener el dueño Carlos González que tiene mascotas asociadas
        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var carlos = duenos!.First(d => d.Dni == "34567890");

        var response = await _client.DeleteAsync($"/api/duenos/{carlos.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
