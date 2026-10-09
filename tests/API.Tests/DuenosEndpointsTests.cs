using System.Net;
using System.Net.Http.Json;
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
    public async Task Create_DatosValidos_Retorna201Created()
    {
        var nuevo = new DuenoCreateDTO
        {
            Nombre = "Federico",
            Apellido = "Romero",
            Dni = "42" + Random.Shared.Next(100000, 999999),
            Telefono = "11-3344-5566",
            Email = $"fede_{Guid.NewGuid():N}@test.com",
            Direccion = "Av. de Mayo 500"
        };

        var response = await _client.PostAsJsonAsync("/api/duenos", nuevo);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<DuenoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(nuevo.Nombre, creado.Nombre);
        Assert.Equal(nuevo.Dni, creado.Dni);
    }

    [Fact]
    public async Task Create_DniDuplicado_Retorna409Conflict()
    {
        // RN-05: DNI ya registrado por DbInitializer
        var duplicado = new DuenoCreateDTO
        {
            Nombre = "Clon",
            Apellido = "González",
            Dni = "34567890", // DNI de Carlos González sembrado
            Telefono = "11-0000-0000",
            Email = "clon@test.com"
        };

        var response = await _client.PostAsJsonAsync("/api/duenos", duplicado);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
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
