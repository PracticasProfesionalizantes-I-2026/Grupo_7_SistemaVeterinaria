using System.Net;
using System.Net.Http.Json;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Mascota;
using Xunit;

namespace API.Tests;

public class AtencionesMedicasEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AtencionesMedicasEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAll_Retorna200OkYListaDeAtenciones()
    {
        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atenciones = await response.Content.ReadFromJsonAsync<List<AtencionMedicaResponseDTO>>();
        Assert.NotNull(atenciones);
        Assert.True(atenciones.Count >= 2); // Sembradas por DbInitializer
    }

    [Fact]
    public async Task Create_DatosValidosConVacunaApto_Retorna201Created()
    {
        // Obtener la mascota activa Milo
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación quíntuple",
            Diagnostico = "Paciente en óptimas condiciones",
            Tratamiento = "Vacuna aplicada",
            PesoRegistrado = 29.0m,
            EsAptoVacunacion = true,
            VacunaAplicada = "Quíntuple Canina",
            Observaciones = "Ninguna reacción adversa"
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal(milo.Id, creada.MascotaId);
        Assert.Equal("Quíntuple Canina", creada.VacunaAplicada);
    }

    [Fact]
    public async Task Create_SobreMascotaInactiva_Retorna409Conflict()
    {
        // RN-07: Rocky está inactiva y no puede recibir nuevas atenciones
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var rocky = mascotas!.First(m => m.Nombre == "Rocky" && !m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = rocky.Id,
            MotivoConsulta = "Consulta",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 32.0m,
            EsAptoVacunacion = false
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_VacunaSinAptitud_Retorna409Conflict()
    {
        // RN-03 / RN-04: No se puede aplicar vacuna si EsAptoVacunacion == false
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación",
            Diagnostico = "Fiebre alta",
            Tratamiento = "Antipirético",
            PesoRegistrado = 28.5m,
            EsAptoVacunacion = false, // NO APTO
            VacunaAplicada = "Rabia"   // Intenta registrar vacuna
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Update_Retorna409Conflict_PorInmutabilidad()
    {
        // RN-01: Inmutabilidad de las atenciones médicas
        var atenciones = await _client.GetFromJsonAsync<List<AtencionMedicaResponseDTO>>("/api/atenciones-medicas");
        var atencion = atenciones!.First();

        var response = await _client.PutAsync($"/api/atenciones-medicas/{atencion.Id}", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Retorna409Conflict_PorInmutabilidad()
    {
        // RN-01: Inmutabilidad de las atenciones médicas
        var atenciones = await _client.GetFromJsonAsync<List<AtencionMedicaResponseDTO>>("/api/atenciones-medicas");
        var atencion = atenciones!.First();

        var response = await _client.DeleteAsync($"/api/atenciones-medicas/{atencion.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
