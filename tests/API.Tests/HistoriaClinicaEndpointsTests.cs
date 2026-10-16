using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.HistoriaClinica;
using Xunit;

namespace API.Tests;

public class HistoriaClinicaEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public HistoriaClinicaEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
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

    private async Task<Guid> ObtenerIdMascotaAsync(string nombre)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        var mascota = await context.Mascotas.FirstOrDefaultAsync(m => m.Nombre == nombre);
        Assert.NotNull(mascota);
        return mascota.Id;
    }

    [Fact]
    public async Task GetHistoriaClinica_ConVeterinario_Retorna200OkYHistoriaCompleta()
    {
        // Arrange
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var miloId = await ObtenerIdMascotaAsync("Milo");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{miloId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hc = await response.Content.ReadFromJsonAsync<HistoriaClinicaResponseDTO>();
        Assert.NotNull(hc);
        Assert.Equal(miloId, hc.MascotaId);
        Assert.Equal("Milo", hc.NombreMascota);
        Assert.Equal("Carlos González", hc.DuenoNombreCompleto);
        Assert.Equal("34567890", hc.DuenoDni);
        Assert.True(hc.Activa);
        Assert.True(hc.TotalAtenciones >= 1);
        Assert.NotEmpty(hc.AtencionesMedicas);
        Assert.NotNull(hc.FechaUltimaVisita);
        Assert.NotEqual("Sin visitas registradas", hc.UltimaVisitaVisualizada);
        Assert.Null(hc.MensajeInformativo);
    }

    [Fact]
    public async Task GetHistoriaClinica_RutaCanonicaMascotas_Retorna200Ok()
    {
        // Arrange: Ruta canónica /api/mascotas/{id}/historia-clinica
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var miloId = await ObtenerIdMascotaAsync("Milo");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/mascotas/{miloId}/historia-clinica");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hc = await response.Content.ReadFromJsonAsync<HistoriaClinicaResponseDTO>();
        Assert.NotNull(hc);
        Assert.Equal(miloId, hc.MascotaId);
        Assert.Equal("Milo", hc.NombreMascota);
    }

    [Fact]
    public async Task GetHistoriaClinica_ConRolAdministrador_Retorna200Ok()
    {
        // Arrange
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var miloId = await ObtenerIdMascotaAsync("Milo");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{miloId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoriaClinica_ConRolRecepcionista_Retorna403Forbidden()
    {
        // Arrange (RN-02 / RBAC: Recepcionista no tiene acceso a historias clínicas)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var miloId = await ObtenerIdMascotaAsync("Milo");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{miloId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoriaClinica_SinToken_Retorna401Unauthorized()
    {
        // Arrange
        var miloId = await ObtenerIdMascotaAsync("Milo");

        // Act
        var response = await _client.GetAsync($"/api/historias-clinicas/{miloId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoriaClinica_MascotaInexistente_Retorna404NotFound()
    {
        // Arrange
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistoriaClinica_MascotaSinAtenciones_Retorna200OkConSinVisitasRegistradas()
    {
        // Arrange (Flujo 3b / INF-07: Luna no tiene atenciones médicas)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var lunaId = await ObtenerIdMascotaAsync("Luna");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{lunaId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hc = await response.Content.ReadFromJsonAsync<HistoriaClinicaResponseDTO>();
        Assert.NotNull(hc);
        Assert.Equal("Luna", hc.NombreMascota);
        Assert.Equal(0, hc.TotalAtenciones);
        Assert.Empty(hc.AtencionesMedicas);
        Assert.Equal("Sin visitas registradas", hc.UltimaVisitaVisualizada);
        Assert.Equal("Sin atenciones médicas registradas a la fecha.", hc.MensajeInformativo);
    }

    [Fact]
    public async Task GetHistoriaClinica_MascotaInactiva_Retorna200OkConHistorialPreservado()
    {
        // Arrange (RN-07: Rocky es mascota inactiva, pero su historia clínica es accesible en solo lectura)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var rockyId = await ObtenerIdMascotaAsync("Rocky");

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/historias-clinicas/{rockyId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hc = await response.Content.ReadFromJsonAsync<HistoriaClinicaResponseDTO>();
        Assert.NotNull(hc);
        Assert.Equal("Rocky", hc.NombreMascota);
        Assert.False(hc.Activa); // Inactiva
        Assert.True(hc.TotalAtenciones >= 1);
        Assert.NotEmpty(hc.AtencionesMedicas);
    }

    [Fact]
    public async Task BuscarHistoriasClinicas_PorNombreMascota_Retorna200Ok()
    {
        // Arrange
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/historias-clinicas?criterio=Milo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<HistoriaClinicaBusquedaDTO>>();
        Assert.NotNull(resultados);
        Assert.Contains(resultados, r => r.NombreMascota == "Milo");
    }

    [Fact]
    public async Task BuscarHistoriasClinicas_PorDniDuenoConMultiplesMascotas_RetornaTodasLasMascotas()
    {
        // Arrange: Carlos González (DNI 34567890) tiene tanto a Milo como a Luna
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/historias-clinicas?criterio=34567890");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<HistoriaClinicaBusquedaDTO>>();
        Assert.NotNull(resultados);
        Assert.True(resultados.Count >= 2);
        Assert.Contains(resultados, r => r.NombreMascota == "Milo");
        Assert.Contains(resultados, r => r.NombreMascota == "Luna");
        Assert.All(resultados, r => Assert.Equal("Carlos González", r.DuenoNombreCompleto));
    }

    [Fact]
    public async Task BuscarHistoriasClinicas_SinParametro_RetornaTodasLasMascotas()
    {
        // Arrange
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/historias-clinicas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var resultados = await response.Content.ReadFromJsonAsync<List<HistoriaClinicaBusquedaDTO>>();
        Assert.NotNull(resultados);
        Assert.True(resultados.Count >= 3);
    }

    [Fact]
    public async Task BuscarHistoriasClinicas_ConRolRecepcionista_Retorna403Forbidden()
    {
        // Arrange
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/historias-clinicas");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
