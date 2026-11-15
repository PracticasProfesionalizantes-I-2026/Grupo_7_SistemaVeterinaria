using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.Auth;
using Shared.DTOs.Mascota;
using Shared.DTOs.Reporte;
using Xunit;

namespace API.Tests;

/// <summary>
/// Recomendación 4: FechaRegistro de mascotas (alta en el sistema) y filtrado por período en reportes.
/// Las mascotas históricas con FechaRegistro NULL no se contabilizan en reportes de altas por período.
/// </summary>
public class MascotasFechaRegistroTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MascotasFechaRegistroTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> ObtenerTokenAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(auth);
        Assert.NotNull(auth!.Token);
        return auth.Token!;
    }

    private async Task<Guid> ObtenerDuenoIdAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        return await context.Duenos.AsNoTracking().Select(d => d.Id).FirstAsync();
    }

    private async Task<MascotaResponseDTO> CrearMascotaViaApiAsync(string nombre)
    {
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/mascotas")
        {
            Content = JsonContent.Create(new MascotaCreateDTO
            {
                DuenoId = await ObtenerDuenoIdAsync(),
                Nombre = nombre,
                Especie = "Perro",
                Raza = "Mestizo",
                Sexo = "Macho",
                FechaNacimiento = DateTime.UtcNow.AddYears(-2),
                Peso = 12m
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(creada);
        return creada!;
    }

    private async Task<Guid> InsertarMascotaDirectaAsync(string nombre, DateTime? fechaRegistro)
    {
        var duenoId = await ObtenerDuenoIdAsync();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        var mascota = new Mascota
        {
            Id = Guid.NewGuid(),
            DuenoId = duenoId,
            Nombre = nombre,
            Especie = "Gato",
            Raza = "Común",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-3),
            Peso = 4m,
            Activa = true,
            FechaRegistro = fechaRegistro
        };
        context.Mascotas.Add(mascota);
        await context.SaveChangesAsync();
        return mascota.Id;
    }

    private async Task<ReporteResponseDTO> GenerarReporteMascotasAsync(DateTime desde, DateTime hasta)
    {
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Altas de mascotas por período",
                TiposReporte = new List<string> { "Mascotas" },
                FechaDesde = desde,
                FechaHasta = hasta
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var reporte = await response.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.NotNull(reporte);
        Assert.NotNull(reporte!.DatosReporte.Mascotas);
        return reporte;
    }

    [Fact]
    public async Task Create_MascotaNueva_AsignaFechaRegistroUtcActual()
    {
        var antes = DateTime.UtcNow.AddSeconds(-1);

        var creada = await CrearMascotaViaApiAsync("Registro-Nueva");

        Assert.NotNull(creada.FechaRegistro);
        Assert.InRange(creada.FechaRegistro!.Value.ToUniversalTime(), antes, DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task Update_ConservaFechaRegistroOriginal()
    {
        var creada = await CrearMascotaViaApiAsync("Registro-Inmutable");
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/mascotas/{creada.Id}")
        {
            Content = JsonContent.Create(new MascotaUpdateDTO
            {
                Nombre = "Registro-Inmutable-Editada",
                Especie = "Perro",
                Raza = "Mestizo",
                Sexo = "Macho",
                FechaNacimiento = creada.FechaNacimiento,
                Peso = 13m
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.NotNull(actualizada);
        Assert.Equal(creada.FechaRegistro, actualizada!.FechaRegistro);
    }

    [Fact]
    public async Task Update_MascotaHistoricaSinFechaRegistro_ConservaNull()
    {
        var id = await InsertarMascotaDirectaAsync("Historica-Update", null);
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/mascotas/{id}")
        {
            Content = JsonContent.Create(new MascotaUpdateDTO
            {
                Nombre = "Historica-Update-Editada",
                Especie = "Gato",
                Raza = "Común",
                Sexo = "Hembra",
                FechaNacimiento = DateTime.UtcNow.AddYears(-3),
                Peso = 4.5m
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizada = await response.Content.ReadFromJsonAsync<MascotaResponseDTO>();
        Assert.Null(actualizada!.FechaRegistro);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        var enBd = await context.Mascotas.AsNoTracking().FirstAsync(m => m.Id == id);
        Assert.Null(enBd.FechaRegistro);
    }

    [Fact]
    public async Task Reporte_Mascotas_CuentaSoloAltasDentroDelPeriodo_ExcluyeFueraDeRangoYNullHistoricos()
    {
        // Rango exclusivo en el pasado lejano: solo cuenta la mascota con alta verificada en ese día.
        var fechaAntigua = new DateTime(2020, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        await InsertarMascotaDirectaAsync("Alta-2020", fechaAntigua);
        await InsertarMascotaDirectaAsync("Historica-Null-A", null);
        await InsertarMascotaDirectaAsync("Historica-Null-B", null);

        var reporte = await GenerarReporteMascotasAsync(
            new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2020, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(1, reporte.TotalMascotas);
        Assert.Equal(1, reporte.DatosReporte.Mascotas!.TotalMascotasRegistradas);
        Assert.Equal(1, reporte.DatosReporte.Mascotas.TotalActivas);
        Assert.Equal(1, reporte.DatosReporte.Mascotas.DistribucionPorEspecie["Gato"]);
    }

    [Fact]
    public async Task Reporte_Mascotas_PeriodoReciente_IncluyeAltaNuevaYExcluyeNullHistoricos()
    {
        var desde = DateTime.UtcNow.AddMinutes(-5);
        await InsertarMascotaDirectaAsync("Historica-Null-C", null);
        var creada = await CrearMascotaViaApiAsync("Alta-Reciente");
        Assert.NotNull(creada.FechaRegistro);

        var reporte = await GenerarReporteMascotasAsync(desde, DateTime.UtcNow);

        Assert.True(reporte.DatosReporte.Mascotas!.TotalMascotasRegistradas >= 1);
        Assert.Equal(reporte.TotalMascotas, reporte.DatosReporte.Mascotas.TotalMascotasRegistradas);

        // Ninguna mascota histórica con FechaRegistro NULL puede estar contabilizada.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        var enRango = await context.Mascotas.AsNoTracking()
            .CountAsync(m => m.FechaRegistro != null && m.FechaRegistro >= desde);
        Assert.Equal(enRango, reporte.DatosReporte.Mascotas.TotalMascotasRegistradas);
    }

    [Fact]
    public async Task Reporte_Mascotas_PeriodoSinAltas_RetornaCeroAunqueExistanMascotasSinFechaRegistro()
    {
        await InsertarMascotaDirectaAsync("Historica-Null-D", null);

        var reporte = await GenerarReporteMascotasAsync(
            new DateTime(2019, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2019, 6, 30, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(0, reporte.TotalMascotas);
        Assert.Equal(0, reporte.DatosReporte.Mascotas!.TotalMascotasRegistradas);
    }
}
