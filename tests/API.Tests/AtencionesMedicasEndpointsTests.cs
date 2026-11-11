using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Context;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Auth;
using Shared.DTOs.Mascota;
using Shared.DTOs.Turno;
using Xunit;

namespace API.Tests;

public class AtencionesMedicasEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AtencionesMedicasEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task AutenticarComoVeterinarioAsync()
    {
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = "vet.carlos",
            Password = "Vet1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(authData);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);
    }

    private async Task AutenticarComoRecepcionistaAsync()
    {
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = "recep.ana",
            Password = "Recep1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(authData);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);
    }

    private async Task AutenticarComoAdminAsync()
    {
        var loginReq = new LoginRequestDTO
        {
            UsuarioOEmail = "admin",
            Password = "Admin1234!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
        response.EnsureSuccessStatusCode();

        var authData = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(authData);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData.Token);
    }

    [Fact]
    public async Task GetAll_ConRolVeterinario_Retorna200OkYListaDeAtenciones()
    {
        await AutenticarComoVeterinarioAsync();

        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atenciones = await response.Content.ReadFromJsonAsync<List<AtencionMedicaResponseDTO>>();
        Assert.NotNull(atenciones);
        Assert.True(atenciones.Count >= 2); // Sembradas por DbInitializer
    }

    [Fact]
    public async Task GetAll_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Existente_ConRolVeterinario_Retorna200Ok()
    {
        await AutenticarComoVeterinarioAsync();

        var atenciones = await _client.GetFromJsonAsync<List<AtencionMedicaResponseDTO>>("/api/atenciones-medicas");
        var primera = atenciones!.First();

        var response = await _client.GetAsync($"/api/atenciones-medicas/{primera.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var obtenida = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(obtenida);
        Assert.Equal(primera.Id, obtenida.Id);
    }

    [Fact]
    public async Task GetById_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var response = await _client.GetAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var response = await _client.GetAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByMascotaId_ConRolVeterinario_Retorna200Ok()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo");

        var response = await _client.GetAsync($"/api/atenciones-medicas/mascota/{milo.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atenciones = await response.Content.ReadFromJsonAsync<List<AtencionMedicaResponseDTO>>();
        Assert.NotNull(atenciones);
        Assert.NotEmpty(atenciones);
    }

    [Fact]
    public async Task GetByMascotaId_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/atenciones-medicas/mascota/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByMascotaId_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var response = await _client.GetAsync($"/api/atenciones-medicas/mascota/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetByMascotaId_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var response = await _client.GetAsync($"/api/atenciones-medicas/mascota/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = Guid.NewGuid(),
            MotivoConsulta = "Consulta general",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 10m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = Guid.NewGuid(),
            MotivoConsulta = "Consulta",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 10m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = Guid.NewGuid(),
            MotivoConsulta = "Consulta",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 10m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_DatosValidosConVacunaApto_Retorna201Created()
    {
        await AutenticarComoVeterinarioAsync();

        // Obtener la mascota activa Milo
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación quíntuple",
            Anamnesis = "Control anual sin síntomas previos",
            ExamenFisico = "Mucosas rosadas, temperatura y auscultación normales",
            Diagnostico = "Paciente en óptimas condiciones",
            Tratamiento = "Vacuna aplicada",
            PesoRegistrado = 28.5m,
            EsAptoVacunacion = true,
            VacunaAplicada = "Quíntuple Canina",
            Observaciones = "Ninguna reacción adversa"
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal(milo.Id, creada.MascotaId);
        Assert.Equal("Control anual sin síntomas previos", creada.Anamnesis);
        Assert.Equal("Mucosas rosadas, temperatura y auscultación normales", creada.ExamenFisico);
        Assert.Equal("Quíntuple Canina", creada.VacunaAplicada);
        Assert.NotEqual(Guid.Empty, creada.VeterinarioId);
    }

    [Fact]
    public async Task Create_VacunacionConFechaProximaDosisFuturaValida_Retorna201CreatedYPersisteVacunacion()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);
        var proximaDosis = DateTime.UtcNow.Date.AddDays(30);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación con refuerzo programado",
            Anamnesis = "Control sanitario anual",
            ExamenFisico = "Constantes fisiológicas dentro de parámetros normales",
            Diagnostico = "Apto para inmunización",
            Tratamiento = "Aplicación de vacuna séxtuple",
            PesoRegistrado = 28.5m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Séxtuple Canina",
                Dosis = "1 ml subcutánea",
                Lote = "LOTE-2026-S6",
                FechaProximaDosis = proximaDosis
            }
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.NotNull(creada.Vacunacion);
        Assert.Equal("Séxtuple Canina", creada.Vacunacion!.Vacuna);
        Assert.Equal("1 ml subcutánea", creada.Vacunacion.Dosis);
        Assert.Equal("LOTE-2026-S6", creada.Vacunacion.Lote);
        Assert.Equal(proximaDosis, creada.Vacunacion.FechaProximaDosis);
    }

    [Theory]
    [InlineData(-30)]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Create_VacunacionConFechaProximaDosisPasadaOHoy_Retorna400BadRequestYSinPersistir(int diasOffset)
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación con fecha próxima dosis inválida",
            Anamnesis = "Control sanitario",
            ExamenFisico = "Examen normal",
            Diagnostico = "Apto",
            Tratamiento = "Vacuna",
            PesoRegistrado = 28.5m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Antirrábica",
                Dosis = "1 ml",
                Lote = "L-2026",
                FechaProximaDosis = DateTime.UtcNow.Date.AddDays(diasOffset)
            }
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }
    }

    [Fact]
    public async Task Create_VacunacionIncompletaOCamposExcedenMaxLength_Retorna400BadRequestYSinPersistir()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        // 1. Vacunación incompleta (sin nombre de vacuna pero con lote y próxima dosis)
        var dtoVacunaIncompleta = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control",
            Anamnesis = "Anamnesis normal",
            ExamenFisico = "Examen físico normal",
            Diagnostico = "Sano",
            Tratamiento = "Control",
            PesoRegistrado = 28.5m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "   ",
                Lote = "LOTE-SIN-VACUNA",
                FechaProximaDosis = DateTime.UtcNow.Date.AddDays(30)
            }
        };

        var respIncompleta = await _client.PostAsJsonAsync("/api/atenciones-medicas", dtoVacunaIncompleta);
        Assert.Equal(HttpStatusCode.BadRequest, respIncompleta.StatusCode);

        // 2. Campo clínico que supera MaxLength
        var dtoExcesoLongitud = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = new string('M', 251),
            Anamnesis = "Anamnesis normal",
            ExamenFisico = "Examen físico normal",
            Diagnostico = "Sano",
            Tratamiento = "Control",
            PesoRegistrado = 28.5m
        };

        var respExceso = await _client.PostAsJsonAsync("/api/atenciones-medicas", dtoExcesoLongitud);
        Assert.Equal(HttpStatusCode.BadRequest, respExceso.StatusCode);

        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }
    }

    [Theory]
    [InlineData("", "Examen físico normal")]
    [InlineData("   ", "Examen físico normal")]
    [InlineData("Anamnesis clínica válida", "")]
    [InlineData("Anamnesis clínica válida", "   ")]
    public async Task Create_AnamnesisOExamenFisicoVaciosOSoloEspacios_Retorna400BadRequestYSinPersistir(
        string anamnesis, string examenFisico)
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Consulta clínica",
            Anamnesis = anamnesis,
            ExamenFisico = examenFisico,
            Diagnostico = "Diagnóstico clínico",
            Tratamiento = "Tratamiento indicado",
            PesoRegistrado = 28.5m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }
    }

    [Fact]
    public async Task Create_SinAnamnesisNiExamenFisicoEnJson_Retorna400BadRequestYSinPersistir()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        var payloadIncompleto = new
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Consulta sin anamnesis ni examen físico",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 28.5m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", payloadIncompleto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }
    }

    [Fact]
    public async Task Create_SobreMascotaInactiva_Retorna409Conflict()
    {
        await AutenticarComoVeterinarioAsync();

        // RN-07: Rocky está inactiva y no puede recibir nuevas atenciones
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var rocky = mascotas!.First(m => m.Nombre == "Rocky" && !m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = rocky.Id,
            MotivoConsulta = "Consulta",
            Anamnesis = "Anamnesis clínica",
            ExamenFisico = "Examen físico",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 32.0m,
            EsAptoVacunacion = false
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_SobreMascotaInexistente_Retorna404NotFound()
    {
        await AutenticarComoVeterinarioAsync();

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = Guid.NewGuid(),
            MotivoConsulta = "Consulta",
            Anamnesis = "Anamnesis clínica",
            ExamenFisico = "Examen físico",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 10.0m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(-2.0)]
    [InlineData(0.0)]
    [InlineData(0.04)]
    [InlineData(200.01)]
    [InlineData(500.0)]
    [InlineData(28.125)]
    public async Task Create_PesoFueraDeRangoOMasDeDosDecimales_Retorna400BadRequestYSinPersistir(decimal pesoInvalido)
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Consulta",
            Anamnesis = "Anamnesis clínica",
            ExamenFisico = "Examen físico",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = pesoInvalido
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }
    }

    [Fact]
    public async Task Create_VacunaSinAptitud_Retorna409Conflict()
    {
        await AutenticarComoVeterinarioAsync();

        // RN-03 / RN-04: No se puede aplicar vacuna si EsAptoVacunacion == false
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Vacunación",
            Anamnesis = "Decaimiento desde hace 12 horas",
            ExamenFisico = "Temperatura rectal 39.9°C",
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
    public async Task Create_AlertaDesnutricion_DisminucionMayorAl10Porciento_ActivaAlertaYNoBloqueaGuardado()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        // Milo tiene atencion previa con peso 28.5 kg. Si registramos 24.0 kg -> disminución = (28.5 - 24.0)/28.5 = 15.79% > 10%
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Pérdida de apetito",
            Anamnesis = "Come menos hace dos semanas",
            ExamenFisico = "Pérdida de masa muscular leve",
            Diagnostico = "Decaimiento general",
            Tratamiento = "Dieta balanceada",
            PesoRegistrado = 24.0m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.True(creada.AlertaDesnutricion);
        Assert.NotNull(creada.PorcentajeDisminucionPeso);
        Assert.True(creada.PorcentajeDisminucionPeso > 10.0m);
        Assert.NotNull(creada.PesoReferenciaUtilizado);
        Assert.NotNull(creada.MensajeAlertaDesnutricion);
    }

    [Fact]
    public async Task Create_AumentoDePeso_RegistraPorcentajeDisminucionCeroYSinAlerta()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var luna = mascotas!.First(m => m.Nombre == "Luna" && m.Activa);

        // Luna aumenta de peso respecto a su referencia previa (~4.2 kg -> 5.0 kg)
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = luna.Id,
            MotivoConsulta = "Control nutricional",
            Anamnesis = "Buen apetito",
            ExamenFisico = "Aumento saludable de peso",
            Diagnostico = "Paciente sano",
            Tratamiento = "Control anual",
            PesoRegistrado = 5.00m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.False(creada.AlertaDesnutricion);
        Assert.Equal(0m, creada.PorcentajeDisminucionPeso);
        Assert.Null(creada.MensajeAlertaDesnutricion);
    }

    [Fact]
    public async Task Create_ChecklistNoAptoSinVacuna_Retorna201CreatedConMotivoClinico()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Evaluación para vacuna",
            Anamnesis = "Hiporexia y decaimiento",
            ExamenFisico = "Hipertermia 40.1°C, mucosas congestivas",
            Diagnostico = "Fiebre persistente",
            Tratamiento = "Medicación antipirética",
            PesoRegistrado = 28.5m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = false,
                BuenEstadoGeneral = false,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = false,
                MotivoNoApto = "Temperatura de 40.1°C"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.False(creada.EsAptoVacunacion);
        Assert.Equal("NO_APTO", creada.AptitudVacunacionResultado);
        Assert.Equal("Temperatura de 40.1°C", creada.MotivoNoAptoVacunacion);
        Assert.Null(creada.VacunaAplicada);
    }

    [Fact]
    public async Task Create_ConProcedimientoQuirurgicoPrescripcionesYEstudios_Retorna201CreatedConDetalles()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var luna = mascotas!.First(m => m.Nombre == "Luna" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = luna.Id,
            MotivoConsulta = "Limpieza dental compleja",
            Anamnesis = "Paciente felino con sarro severo",
            ExamenFisico = "Gingivitis moderada, resto normal",
            Diagnostico = "Periodontitis felina grado II",
            Tratamiento = "Profilaxis ultrasónica bajo sedación",
            PesoRegistrado = 4.2m,
            ProcedimientoQuirurgico = new ProcedimientoQuirurgicoCreateDTO
            {
                TipoProcedimiento = "Limpieza Quirúrgica Dental",
                Descripcion = "Destartraje subgingival y pulido de piezas dentales",
                ObservacionesComplicaciones = "Sin sangrado ni complicaciones anestésicas"
            },
            Prescripciones = new List<PrescripcionCreateDTO>
            {
                new PrescripcionCreateDTO
                {
                    Medicamento = "Clindamicina 25mg",
                    Dosis = "1 comprimido",
                    Frecuencia = "Cada 12 hs",
                    Duracion = "5 días"
                }
            },
            Estudios = new List<EstudioComplementarioCreateDTO>
            {
                new EstudioComplementarioCreateDTO
                {
                    Nombre = "Radiografía intraoral",
                    Resultado = "Sin pérdida ósea alveolar significativa"
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.NotNull(creada.ProcedimientoQuirurgico);
        Assert.Equal("Limpieza Quirúrgica Dental", creada.ProcedimientoQuirurgico.TipoProcedimiento);
        Assert.Single(creada.Prescripciones);
        Assert.Equal("Clindamicina 25mg", creada.Prescripciones[0].Medicamento);
        Assert.Single(creada.Estudios);
        Assert.Equal("Radiografía intraoral", creada.Estudios[0].Nombre);
    }

    [Fact]
    public async Task Create_EndpointAlternativo_ApiAtenciones_Retorna201Created()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Consulta de control dermatológico",
            Anamnesis = "Prurito controlado con baños medicados",
            ExamenFisico = "Piel sin lesiones activas ni eritema",
            Diagnostico = "Dermatitis atópica controlada",
            Tratamiento = "Champú hipoalergénico semanal",
            PesoRegistrado = 28.5m
        };

        // Ruta alternativa: /api/atenciones
        var response = await _client.PostAsJsonAsync("/api/atenciones", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal(milo.Id, creada.MascotaId);
    }

    [Fact]
    public async Task Create_SeguimientoCronico_Retorna201CreatedYTurnosSeguimientoOFechasPendientes()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control periódico de enfermedad crónica",
            Anamnesis = "Control cardiológico de rutina sin síncopes",
            ExamenFisico = "Frecuencia cardíaca estable, mucosas rosadas",
            Diagnostico = "Cardiopatía dilatada canina",
            Tratamiento = "Enalapril y chequeos mensuales",
            PesoRegistrado = 28.5m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal(milo.Id, creada.MascotaId);
        Assert.NotNull(creada.MensajeSeguimientoCronico);
    }

    [Fact]
    public async Task Create_SeguimientoCronico_ConVeterinarioConDisponibilidad_PersisteTurnosYRetorna201CreatedSinConflictoTracking()
    {
        await AutenticarComoVeterinarioAsync();

        // 1. Configurar disponibilidad del veterinario vet.carlos en la BD en memoria para habilitar la generación de turnos
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var vet = await db.Usuarios.FirstAsync(u => u.Username == "vet.carlos");

            if (!await db.DisponibilidadesVeterinarios.AnyAsync(d => d.VeterinarioId == vet.Id))
            {
                for (int dia = 0; dia <= 6; dia++)
                {
                    db.DisponibilidadesVeterinarios.Add(new DisponibilidadVeterinario
                    {
                        Id = Guid.NewGuid(),
                        VeterinarioId = vet.Id,
                        DiaSemana = (DayOfWeek)dia,
                        Jornada = "Tarde",
                        HoraInicio = new TimeSpan(14, 0, 0),
                        HoraFin = new TimeSpan(18, 0, 0),
                        Activo = true
                    });
                }
                await db.SaveChangesAsync();
            }
        }

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control endocrinológico y ajuste de insulina (RN-11)",
            Anamnesis = "Control periódico de diabetes",
            ExamenFisico = "Paciente estable",
            Diagnostico = "Diabetes Mellitus canina",
            Tratamiento = "Insulina NPH y controles mensuales",
            PesoRegistrado = 28.5m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        // Act: La atención médica se persiste y se generan turnos de seguimiento crónico en la misma petición
        var response = await _client.PostAsJsonAsync("/api/atenciones", dto);

        // Assert: NO debe arrojar HTTP 500 por conflicto de tracking de Mascota, debe retornar 201 Created
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Equal(milo.Id, creada.MascotaId);
        Assert.NotEmpty(creada.TurnosSeguimiento);
        Assert.All(creada.TurnosSeguimiento, t => Assert.NotEqual(Guid.Empty, t.Id));

        // Verificar en la BD que los turnos fueron persistidos en SQLite vinculados a la atención médica
        using (var scopeVerif = _factory.Services.CreateScope())
        {
            var dbVerif = scopeVerif.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var turnosPersistidos = await dbVerif.Turnos
                .Where(t => t.AtencionMedicaId == creada.Id)
                .ToListAsync();

            Assert.NotEmpty(turnosPersistidos);
            Assert.Equal(creada.TurnosSeguimiento.Count, turnosPersistidos.Count);
        }
    }

    [Fact]
    public async Task Update_ConRolVeterinario_Retorna409Conflict_PorInmutabilidad_SinModificarDatos()
    {
        // RN-01: Inmutabilidad de las atenciones médicas
        await AutenticarComoVeterinarioAsync();

        var atenciones = await _client.GetFromJsonAsync<List<AtencionMedicaResponseDTO>>("/api/atenciones-medicas");
        var original = atenciones!.First();

        var response = await _client.PutAsync($"/api/atenciones-medicas/{original.Id}", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Comprobar que el registro médico permanece intacto
        var postIntento = await _client.GetFromJsonAsync<AtencionMedicaResponseDTO>($"/api/atenciones-medicas/{original.Id}");
        Assert.NotNull(postIntento);
        Assert.Equal(original.Id, postIntento.Id);
        Assert.Equal(original.Diagnostico, postIntento.Diagnostico);
        Assert.Equal(original.Tratamiento, postIntento.Tratamiento);
        Assert.Equal(original.PesoRegistrado, postIntento.PesoRegistrado);
    }

    [Fact]
    public async Task Update_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PutAsync($"/api/atenciones-medicas/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var response = await _client.PutAsync($"/api/atenciones-medicas/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var response = await _client.PutAsync($"/api/atenciones-medicas/{Guid.NewGuid()}", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ConRolVeterinario_Retorna409Conflict_PorInmutabilidad_SinEliminarDatos()
    {
        // RN-01: Inmutabilidad de las atenciones médicas
        await AutenticarComoVeterinarioAsync();

        var atenciones = await _client.GetFromJsonAsync<List<AtencionMedicaResponseDTO>>("/api/atenciones-medicas");
        var original = atenciones!.First();

        var response = await _client.DeleteAsync($"/api/atenciones-medicas/{original.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        // Comprobar que el registro médico no fue eliminado y permanece intacto
        var getResponse = await _client.GetAsync($"/api/atenciones-medicas/{original.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var conservada = await getResponse.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(conservada);
        Assert.Equal(original.Id, conservada.Id);
        Assert.Equal(original.Diagnostico, conservada.Diagnostico);
    }

    [Fact]
    public async Task Delete_SinToken_Retorna401Unauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ConRolRecepcionista_Retorna403Forbidden()
    {
        await AutenticarComoRecepcionistaAsync();

        var response = await _client.DeleteAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ConRolAdministrador_Retorna403Forbidden()
    {
        await AutenticarComoAdminAsync();

        var response = await _client.DeleteAsync($"/api/atenciones-medicas/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_IncoherenciaEntreEsEnfermedadCronicaYSeguimientoCronico_Retorna400BadRequest_SinPersistir()
    {
        // Arrange (BUG-06)
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        int totalAntes;
        using (var scopeAntes = _factory.Services.CreateScope())
        {
            var dbAntes = scopeAntes.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            totalAntes = await dbAntes.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
        }

        // Caso 1: EsEnfermedadCronica = true pero SeguimientoCronico = null
        var dtoSinSeguimiento = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control crónico",
            Anamnesis = "Anamnesis",
            ExamenFisico = "Examen físico",
            Diagnostico = "Patología crónica",
            Tratamiento = "Tratamiento crónico",
            PesoRegistrado = 28.5m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = null
        };

        var res1 = await _client.PostAsJsonAsync("/api/atenciones-medicas", dtoSinSeguimiento);
        Assert.Equal(HttpStatusCode.BadRequest, res1.StatusCode);

        // Caso 2: EsEnfermedadCronica = false pero SeguimientoCronico != null
        var dtoContradictorio = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control agudo",
            Anamnesis = "Anamnesis",
            ExamenFisico = "Examen físico",
            Diagnostico = "Cuadro agudo",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 28.5m,
            EsEnfermedadCronica = false,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        var res2 = await _client.PostAsJsonAsync("/api/atenciones-medicas", dtoContradictorio);
        Assert.Equal(HttpStatusCode.BadRequest, res2.StatusCode);

        // Verificar que ninguna atención inválida fue persistida
        using (var scopeDespues = _factory.Services.CreateScope())
        {
            var dbDespues = scopeDespues.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var totalDespues = await dbDespues.AtencionesMedicas.CountAsync(a => a.MascotaId == milo.Id);
            Assert.Equal(totalAntes, totalDespues);
        }

        // Caso 3: EsEnfermedadCronica = true con SeguimientoCronico válido -> 201 Created
        var dtoValidoConTrue = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Control crónico válido",
            Anamnesis = "Anamnesis",
            ExamenFisico = "Examen físico",
            Diagnostico = "Patología crónica",
            Tratamiento = "Tratamiento crónico",
            PesoRegistrado = 28.5m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "  Mensual  ",
                JornadaPreferida = "  Manana  "
            }
        };

        var res3 = await _client.PostAsJsonAsync("/api/atenciones-medicas", dtoValidoConTrue);
        Assert.Equal(HttpStatusCode.Created, res3.StatusCode);
    }

    [Fact]
    public async Task Create_FalloEnGeneracionSeguimiento_RevierteTransaccionYNoDejaEscriturasParcialesEnSQLite()
    {
        // 1. Crear factory y client con ITurnoService que falla durante persistencia de seguimientos crónicos
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITurnoService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<ITurnoService, FailingTurnoService>();
            });
        });

        var customClient = customFactory.CreateClient();

        // Autenticar en customFactory
        var loginRes = await customClient.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = "vet.carlos",
            Password = "Vet1234!"
        });
        loginRes.EnsureSuccessStatusCode();
        var authData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        customClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        // 2. Obtener mascota activa y su FechaUltimaVisita inicial de la BD de customFactory
        DateTime? fechaVisitaInicial;
        Guid mascotaId;
        using (var scope = customFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var mascota = await db.Mascotas.FirstAsync(m => m.Activa);
            mascotaId = mascota.Id;
            fechaVisitaInicial = mascota.FechaUltimaVisita;
        }

        var motivoUnico = "Fallo Transaccional Test " + Guid.NewGuid();
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = motivoUnico,
            Anamnesis = "Anamnesis control atómico",
            ExamenFisico = "Examen clínico normal",
            Diagnostico = "Diagnóstico prueba atómica",
            Tratamiento = "Tratamiento prueba",
            PesoRegistrado = 15m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        // Act: Enviar la petición que fallará al persistir seguimientos
        var response = await customClient.PostAsJsonAsync("/api/atenciones", dto);

        // Assert: No debe ser exitoso (falla propagada por la excepción técnica)
        Assert.False(response.IsSuccessStatusCode);

        // 3. Verificar directamente en SQLite que NO quedó NINGUNA escritura parcial
        using (var scopeVerif = customFactory.Services.CreateScope())
        {
            var dbVerif = scopeVerif.ServiceProvider.GetRequiredService<VeterinariaDbContext>();

            // a. La atención médica NO debe existir en la BD
            var atencionPersistida = await dbVerif.AtencionesMedicas
                .FirstOrDefaultAsync(a => a.MotivoConsulta == motivoUnico);
            Assert.Null(atencionPersistida);

            // b. Ningún turno de seguimiento debe haberse creado vinculado a esa atención
            var turnosPersistidos = await dbVerif.Turnos
                .Where(t => t.MascotaId == mascotaId && t.TipoOrigen == "SeguimientoCronico" && t.FechaCreacion >= DateTime.UtcNow.AddMinutes(-5))
                .ToListAsync();
            Assert.Empty(turnosPersistidos);

            // c. FechaUltimaVisita de la mascota NO debe haberse modificado (reversión atómica)
            var mascotaActualizada = await dbVerif.Mascotas.FindAsync(mascotaId);
            Assert.NotNull(mascotaActualizada);
            Assert.Equal(fechaVisitaInicial, mascotaActualizada.FechaUltimaVisita);
        }
    }

    [Fact]
    public async Task Create_AtencionSinSeguimientoCronico_PersisteCorrectamenteEnTransaccion()
    {
        // Arrange
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var motivoConsulta = "Control simple sin cronico " + Guid.NewGuid();
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = motivoConsulta,
            Anamnesis = "Revisión general anual",
            ExamenFisico = "Normal",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 28m
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/atenciones", dto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creada = await response.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);
        Assert.Empty(creada.TurnosSeguimiento);

        // Verificar persistencia en SQLite
        using (var scopeVerif = _factory.Services.CreateScope())
        {
            var dbVerif = scopeVerif.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var persistida = await dbVerif.AtencionesMedicas.FindAsync(creada.Id);
            Assert.NotNull(persistida);
            Assert.Equal(motivoConsulta, persistida.MotivoConsulta);

            var mascotaVerif = await dbVerif.Mascotas.FindAsync(milo.Id);
            Assert.NotNull(mascotaVerif);
            Assert.NotNull(mascotaVerif.FechaUltimaVisita);
        }
    }

    [Fact]
    public async Task Create_ReintentoTrasFalloDeSeguimiento_PersisteLimpiamenteSinDuplicarAtencion()
    {
        // 1. Configurar un factory donde la primera llamada a generar turnos falla, pero el reintento triunfa
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ITurnoService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }
                services.AddScoped<ITurnoService, FailFirstThenSucceedTurnoService>();
            });
        });

        var client = customFactory.CreateClient();

        // Autenticar en este factory
        var loginRes = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = "vet.carlos",
            Password = "Vet1234!"
        });
        loginRes.EnsureSuccessStatusCode();
        var authData = await loginRes.Content.ReadFromJsonAsync<LoginResponseDTO>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.Token);

        Guid mascotaId;
        using (var scope = customFactory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var vet = await db.Usuarios.FirstAsync(u => u.Username == "vet.carlos");
            if (!await db.DisponibilidadesVeterinarios.AnyAsync(d => d.VeterinarioId == vet.Id))
            {
                for (int dia = 0; dia <= 6; dia++)
                {
                    db.DisponibilidadesVeterinarios.Add(new DisponibilidadVeterinario
                    {
                        Id = Guid.NewGuid(),
                        VeterinarioId = vet.Id,
                        DiaSemana = (DayOfWeek)dia,
                        Jornada = "Tarde",
                        HoraInicio = new TimeSpan(14, 0, 0),
                        HoraFin = new TimeSpan(18, 0, 0),
                        Activo = true
                    });
                }
                await db.SaveChangesAsync();
            }
            var mascota = await db.Mascotas.FirstAsync(m => m.Activa);
            mascotaId = mascota.Id;
        }

        var motivoConsulta = "Reintento Post Fallo Test " + Guid.NewGuid();
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = motivoConsulta,
            Anamnesis = "Control crónico reintento",
            ExamenFisico = "Examen clínico normal",
            Diagnostico = "Cardiopatía",
            Tratamiento = "Medicación diaria",
            PesoRegistrado = 28m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        // Act 1: Primer intento falla en la persistencia de turnos -> Rollback de la transacción
        FailFirstThenSucceedTurnoService.Reset();
        var respuestaFallo = await client.PostAsJsonAsync("/api/atenciones", dto);
        Assert.False(respuestaFallo.IsSuccessStatusCode);

        // Comprobar que en la base de datos no quedó guardada la atención médica
        using (var scopeVerifFallo = customFactory.Services.CreateScope())
        {
            var db = scopeVerifFallo.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var atencionNoGuardada = await db.AtencionesMedicas.FirstOrDefaultAsync(a => a.MotivoConsulta == motivoConsulta);
            Assert.Null(atencionNoGuardada);
        }

        // Act 2: Segundo intento (Reintento de la solicitud con el mismo payload) -> Triunfa
        var respuestaReintento = await client.PostAsJsonAsync("/api/atenciones", dto);
        Assert.Equal(HttpStatusCode.Created, respuestaReintento.StatusCode);

        var creada = await respuestaReintento.Content.ReadFromJsonAsync<AtencionMedicaResponseDTO>();
        Assert.NotNull(creada);

        // 3. Comprobar que en la base de datos existe exactamente UNA atención (sin duplicados) y sus turnos
        using (var scopeVerif = customFactory.Services.CreateScope())
        {
            var db = scopeVerif.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var atenciones = await db.AtencionesMedicas
                .Where(a => a.MotivoConsulta == motivoConsulta)
                .ToListAsync();

            Assert.Single(atenciones);
            Assert.Equal(creada.Id, atenciones[0].Id);

            var turnosPersistidos = await db.Turnos
                .Where(t => t.AtencionMedicaId == creada.Id)
                .ToListAsync();
            Assert.NotEmpty(turnosPersistidos);
        }
    }

    private class FailingTurnoService : ITurnoService
    {
        public Task<TurnoResponseDTO> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<TurnoResponseDTO>> GetAgendaAsync(AgendaFilterDTO filtro) => throw new NotImplementedException();
        public Task<TurnoResponseDTO> CreateManualAsync(TurnoCreateDTO dto) => throw new NotImplementedException();
        public Task<TurnoResponseDTO> UpdateAsync(Guid id, TurnoUpdateDTO dto) => throw new NotImplementedException();
        public Task<TurnoResponseDTO> ConfirmarAsync(Guid id, Guid usuarioId, string username) => throw new NotImplementedException();
        public Task<TurnoResponseDTO> FinalizarAsync(Guid id, Guid usuarioId, string username) => throw new NotImplementedException();
        public Task<TurnoResponseDTO> CancelarAsync(Guid id, TurnoCancelarDTO dto, Guid usuarioId, string username) => throw new NotImplementedException();
        public Task<IEnumerable<DisponibilidadVeterinarioDTO>> GetDisponibilidadVeterinarioAsync(Guid veterinarioId)
        {
            return Task.FromResult<IEnumerable<DisponibilidadVeterinarioDTO>>(new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO { DiaSemana = DayOfWeek.Monday, Jornada = "Tarde" }
            });
        }
        public Task<IEnumerable<DisponibilidadVeterinarioDTO>> ConfigurarDisponibilidadVeterinarioAsync(Guid veterinarioId, ConfigurarDisponibilidadDTO dto) => throw new NotImplementedException();
        public Task<GenerarTurnosCronicosResponseDTO> GenerarTurnosSeguimientoCronicoAsync(GenerarTurnosCronicosDTO dto)
        {
            throw new DbUpdateException("Falla técnica simulada en SQLite al persistir turnos de seguimiento crónico", new Exception());
        }
    }

    private class FailFirstThenSucceedTurnoService : ITurnoService
    {
        private readonly TurnoService _realService;
        private static int _failCount = 1;

        public static void Reset() => _failCount = 1;

        public FailFirstThenSucceedTurnoService(
            ITurnoRepository turnoRepository,
            IDisponibilidadVeterinarioRepository disponibilidadRepository,
            IMascotaRepository mascotaRepository,
            IUsuarioRepository usuarioRepository)
        {
            _realService = new TurnoService(turnoRepository, disponibilidadRepository, mascotaRepository, usuarioRepository);
        }

        public Task<TurnoResponseDTO> GetByIdAsync(Guid id) => _realService.GetByIdAsync(id);
        public Task<IEnumerable<TurnoResponseDTO>> GetAgendaAsync(AgendaFilterDTO filtro) => _realService.GetAgendaAsync(filtro);
        public Task<TurnoResponseDTO> CreateManualAsync(TurnoCreateDTO dto) => _realService.CreateManualAsync(dto);
        public Task<TurnoResponseDTO> UpdateAsync(Guid id, TurnoUpdateDTO dto) => _realService.UpdateAsync(id, dto);
        public Task<TurnoResponseDTO> ConfirmarAsync(Guid id, Guid usuarioId, string username) => _realService.ConfirmarAsync(id, usuarioId, username);
        public Task<TurnoResponseDTO> FinalizarAsync(Guid id, Guid usuarioId, string username) => _realService.FinalizarAsync(id, usuarioId, username);
        public Task<TurnoResponseDTO> CancelarAsync(Guid id, TurnoCancelarDTO dto, Guid usuarioId, string username) => _realService.CancelarAsync(id, dto, usuarioId, username);
        public Task<IEnumerable<DisponibilidadVeterinarioDTO>> GetDisponibilidadVeterinarioAsync(Guid veterinarioId) => _realService.GetDisponibilidadVeterinarioAsync(veterinarioId);
        public Task<IEnumerable<DisponibilidadVeterinarioDTO>> ConfigurarDisponibilidadVeterinarioAsync(Guid veterinarioId, ConfigurarDisponibilidadDTO dto) => _realService.ConfigurarDisponibilidadVeterinarioAsync(veterinarioId, dto);

        public Task<GenerarTurnosCronicosResponseDTO> GenerarTurnosSeguimientoCronicoAsync(GenerarTurnosCronicosDTO dto)
        {
            if (Interlocked.Exchange(ref _failCount, 0) == 1)
            {
                throw new DbUpdateException("Falla técnica simulada en primer intento", new Exception());
            }
            return _realService.GenerarTurnosSeguimientoCronicoAsync(dto);
        }
    }
}
