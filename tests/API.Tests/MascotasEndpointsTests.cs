using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.Dueno;
using Shared.DTOs.Mascota;
using Xunit;

namespace API.Tests;

public class MascotasEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public MascotasEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Retorna200OkYListaDeMascotas()
    {
        var response = await _client.GetAsync("/api/mascotas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var mascotas = await response.Content.ReadFromJsonAsync<List<MascotaResponseDTO>>();
        Assert.NotNull(mascotas);
        Assert.True(mascotas.Count >= 3); // Sembradas por DbInitializer
    }

    [Fact]
    public async Task GetById_NoExistente_Retorna404NotFound()
    {
        var idInexistente = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/mascotas/{idInexistente}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_ConDueñoValido_Retorna201Created()
    {
        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        var nuevaMascota = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "Toby",
            Especie = "Perro",
            Raza = "Labrador",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 20.0m,
            Observaciones = "Cachorro sano"
        };

        var response = await _client.PostAsJsonAsync("/api/mascotas", nuevaMascota);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal("Toby", creada.Nombre);
        Assert.True(creada.Activa);
    }

    [Fact]
    public async Task Desactivar_MascotaActiva_Retorna200OkYBajaLogica()
    {
        // MEJ-03: Baja lógica exitosa debe retornar 200 OK con confirmación y estado Inactiva
        var duenos = await _client.GetFromJsonAsync<List<DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        // Creamos una mascota exclusivamente para esta prueba
        var nueva = new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "ParaDesactivar",
            Especie = "Gato",
            Raza = "Criollo",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 3.5m
        };
        var resCreate = await _client.PostAsJsonAsync("/api/mascotas", nueva);
        var mascotaCreada = await resCreate.Content.ReadFromJsonAsync<MascotaResponseDTO>();

        // Act: Llamar al endpoint de baja lógica
        var response = await _client.PatchAsync($"/api/mascotas/{mascotaCreada!.Id}/desactivar", null);

        // Assert: 200 OK y estado Inactiva
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var desactivada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(desactivada);
        Assert.False(desactivada.Activa);
    }

    [Fact]
    public async Task Desactivar_MascotaYaInactiva_Retorna409Conflict()
    {
        // MEJ-03: Rocky fue sembrada con Activa = false
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var rocky = mascotas!.First(m => m.Nombre == "Rocky" && !m.Activa);

        // Act: Intentar desactivar mascota previamente inactiva
        var response = await _client.PatchAsync($"/api/mascotas/{rocky.Id}/desactivar", null);

        // Assert: 409 Conflict
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
