using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Auth;
using Shared.DTOs.Mascota;
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
    public async Task GetAll_Retorna200OkYListaDeAtenciones()
    {
        var response = await _client.GetAsync("/api/atenciones-medicas");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var atenciones = await response.Content.ReadFromJsonAsync<List<AtencionMedicaResponseDTO>>();
        Assert.NotNull(atenciones);
        Assert.True(atenciones.Count >= 2); // Sembradas por DbInitializer
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
        Assert.Equal("Quíntuple Canina", creada.VacunaAplicada);
        Assert.NotEqual(Guid.Empty, creada.VeterinarioId);
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
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 10.0m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_PesoCeroONegativo_Retorna400BadRequest()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Consulta",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = -2.0m
        };

        var response = await _client.PostAsJsonAsync("/api/atenciones-medicas", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task Create_ChecklistNoAptoSinVacuna_Retorna201CreatedConMotivoClinico()
    {
        await AutenticarComoVeterinarioAsync();

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var milo = mascotas!.First(m => m.Nombre == "Milo" && m.Activa);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = milo.Id,
            MotivoConsulta = "Evaluación para vacuna",
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
