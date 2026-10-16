using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.DTOs.Auth;
using Shared.DTOs.Reporte;
using Xunit;

namespace API.Tests;

public class ReportesEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportesEndpointsTests(CustomWebApplicationFactory factory)
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
    public async Task GenerarReporte_ComoAdministrador_Retorna201CreatedYLocation()
    {
        // Arrange (CU-11 Flujo Principal)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Reporte mensual de control administrativo",
                TiposReporte = new List<string> { "Atenciones", "Vacunaciones", "Mascotas", "Duenos", "Turnos" },
                FechaDesde = DateTime.UtcNow.AddMonths(-1),
                FechaHasta = DateTime.UtcNow
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var reporte = await response.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.NotNull(reporte);
        Assert.NotEqual(Guid.Empty, reporte.Id);
        Assert.Equal("Reporte mensual de control administrativo", reporte.Motivo);
        Assert.NotNull(reporte.DatosReporte);
        Assert.NotNull(reporte.DatosReporte.Atenciones);
        Assert.NotNull(reporte.DatosReporte.Vacunaciones);
        Assert.NotNull(reporte.DatosReporte.Mascotas);
        Assert.NotNull(reporte.DatosReporte.Duenos);
        Assert.NotNull(reporte.DatosReporte.Turnos);
    }

    [Fact]
    public async Task GenerarReporte_ComoVeterinario_Retorna403Forbidden()
    {
        // Arrange (CU-11 Flujo alternativo 2a)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Intento no autorizado de generación",
                TiposReporte = new List<string> { "Atenciones" },
                FechaDesde = DateTime.UtcNow.AddMonths(-1),
                FechaHasta = DateTime.UtcNow
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerarReporte_ComoRecepcionista_Retorna403Forbidden()
    {
        // Arrange (CU-11 Flujo alternativo 2a)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Intento no autorizado",
                TiposReporte = new List<string> { "Duenos" },
                FechaDesde = DateTime.UtcNow.AddMonths(-1),
                FechaHasta = DateTime.UtcNow
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GenerarReporte_SinToken_Retorna401Unauthorized()
    {
        // Arrange (CU-11 Flujo alternativo 1a)
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Sin autenticar",
                TiposReporte = new List<string> { "Atenciones" },
                FechaDesde = DateTime.UtcNow.AddMonths(-1),
                FechaHasta = DateTime.UtcNow
            })
        };

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GenerarReporte_RangoFechasInvertido_Retorna400BadRequest()
    {
        // Arrange (CU-11 Flujo alternativo 3a)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Fechas invertidas",
                TiposReporte = new List<string> { "Atenciones" },
                FechaDesde = DateTime.UtcNow.AddDays(10),
                FechaHasta = DateTime.UtcNow
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenerarReporte_CamposObligatoriosIncompletos_Retorna400BadRequest()
    {
        // Arrange (CU-11 Flujo alternativo 2b)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "", // Vacío
                TiposReporte = new List<string>(), // Vacío
                FechaDesde = DateTime.UtcNow.AddDays(-10),
                FechaHasta = DateTime.UtcNow
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReportes_ComoAdministrador_Retorna200OKYListaHistorica()
    {
        // Arrange (CU-10 Sub-variación 1)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");

        // Primero generamos un reporte para garantizar contenido
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Reporte para historial",
                TiposReporte = new List<string> { "Atenciones", "Turnos" },
                FechaDesde = DateTime.UtcNow.AddDays(-15),
                FechaHasta = DateTime.UtcNow
            })
        };
        postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postResponse = await _client.SendAsync(postRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/reportes");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(getRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reportes = await response.Content.ReadFromJsonAsync<List<ReporteResumenDTO>>();
        Assert.NotNull(reportes);
        Assert.NotEmpty(reportes);
        Assert.Contains(reportes, r => r.Motivo == "Reporte para historial");
    }

    [Fact]
    public async Task GetReportes_ComoVeterinario_Retorna403Forbidden()
    {
        // Arrange (CU-10 Flujo alternativo 2a)
        var token = await ObtenerTokenAsync("vet.carlos", "Vet1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/reportes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReportes_ComoRecepcionista_Retorna403Forbidden()
    {
        // Arrange (CU-10 Flujo alternativo 2a)
        var token = await ObtenerTokenAsync("recep.ana", "Recep1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/reportes");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetReporteById_ReporteExistente_Retorna200OKYDetalleConsolidado()
    {
        // Arrange (CU-10 Flujo Principal)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");

        // 1. Generar reporte
        var postRequest = new HttpRequestMessage(HttpMethod.Post, "/api/reportes/generar")
        {
            Content = JsonContent.Create(new GenerarReporteRequestDTO
            {
                Motivo = "Auditoría semestral",
                TiposReporte = new List<string> { "Atenciones", "Vacunaciones" },
                FechaDesde = DateTime.UtcNow.AddMonths(-6),
                FechaHasta = DateTime.UtcNow
            })
        };
        postRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var postResponse = await _client.SendAsync(postRequest);
        var creado = await postResponse.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.NotNull(creado);

        // 2. Consultar reporte por su ID
        var getRequest = new HttpRequestMessage(HttpMethod.Get, $"/api/reportes/{creado.Id}");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var getResponse = await _client.SendAsync(getRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var obtenido = await getResponse.Content.ReadFromJsonAsync<ReporteResponseDTO>();
        Assert.NotNull(obtenido);
        Assert.Equal(creado.Id, obtenido.Id);
        Assert.Equal("Auditoría semestral", obtenido.Motivo);
        Assert.NotNull(obtenido.DatosReporte.Atenciones);
        Assert.NotNull(obtenido.DatosReporte.Vacunaciones);
    }

    [Fact]
    public async Task GetReporteById_ReporteInexistente_Retorna404NotFound()
    {
        // Arrange (CU-10 Flujo alternativo 3a)
        var token = await ObtenerTokenAsync("admin", "Admin1234!");
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/reportes/{Guid.NewGuid()}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReporteById_SinToken_Retorna401Unauthorized()
    {
        // Arrange (CU-10 Flujo alternativo 1a)
        var response = await _client.GetAsync($"/api/reportes/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
