using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.DTOs.Auth;
using Shared.DTOs.Mascota;
using Shared.DTOs.Turno;
using Shared.Security;
using Xunit;

namespace API.Tests;

public class TurnosEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TurnosEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<Guid> LoginAndSetHeaderAsync(string username, string password)
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(auth);
        Assert.NotNull(auth.Token);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
        var idClaim = jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.NameId).Value;
        return Guid.Parse(idClaim);
    }

    private async Task<Usuario> EnsureSecondVeterinarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
        var vetLaura = await db.Usuarios.FirstOrDefaultAsync(u => u.Username == "vet.laura");
        if (vetLaura == null)
        {
            vetLaura = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "vet.laura",
                Email = "laura.veterinaria@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("VetLaura1234!"),
                Rol = "Veterinario",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };
            db.Usuarios.Add(vetLaura);
            await db.SaveChangesAsync();
        }
        return vetLaura;
    }

    private static ConfigurarDisponibilidadDTO CrearDisponibilidadValidaDto(Guid veterinarioId, TimeSpan horaInicioManana)
    {
        return new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = veterinarioId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = horaInicioManana,
                    HoraFin = new TimeSpan(12, 0, 0)
                },
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Jornada = "Tarde",
                    HoraInicio = new TimeSpan(14, 0, 0),
                    HoraFin = new TimeSpan(18, 0, 0)
                }
            }
        };
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_VeterinarioModificandoSuPropiaDisponibilidad_Retorna200OK_YPersisteCambios(string metodoHttp)
    {
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dto = CrearDisponibilidadValidaDto(vetId, new TimeSpan(8, 30, 0));

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dto)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<DisponibilidadVeterinarioDTO>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, d => d.DiaSemana == DayOfWeek.Monday && d.HoraInicio == new TimeSpan(8, 30, 0));

        // Verificar que la disponibilidad quedó efectivamente almacenada
        var getResponse = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetId}");
        Assert.NotNull(getResponse);
        Assert.Equal(2, getResponse.Count);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_VeterinarioIntentandoModificarDisponibilidadDeOtro_Retorna403Forbidden_SinAlterarDatos(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();

        // Primero la Recepcionista configura una disponibilidad conocida para vet.laura
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var dispOriginal = CrearDisponibilidadValidaDto(vetLaura.Id, new TimeSpan(9, 0, 0));
        var setupRes = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dispOriginal);
        Assert.Equal(HttpStatusCode.OK, setupRes.StatusCode);

        // Ahora vet.carlos inicia sesión e intenta sobrescribir la disponibilidad de vet.laura
        await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var intentoIntruso = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Friday,
                    Jornada = "Tarde",
                    HoraInicio = new TimeSpan(16, 0, 0),
                    HoraFin = new TimeSpan(17, 0, 0)
                }
            }
        };

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoIntruso)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoIntruso);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Verificar que la disponibilidad almacenada de vet.laura no fue modificada
        var guardada = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetLaura.Id}");
        Assert.NotNull(guardada);
        Assert.Equal(2, guardada.Count);
        Assert.Contains(guardada, d => d.DiaSemana == DayOfWeek.Monday && d.HoraInicio == new TimeSpan(9, 0, 0));
        Assert.DoesNotContain(guardada, d => d.DiaSemana == DayOfWeek.Friday);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_VeterinarioEnviandoOtroVeterinarioIdEnBody_Retorna403Forbidden_SinAlterarDatos(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        // Configurar estado base de vet.carlos
        var dispBase = CrearDisponibilidadValidaDto(vetCarlosId, new TimeSpan(8, 0, 0));
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", dispBase);

        // Intenta enviar en el body el ID de otro veterinario
        var dtoInconsistente = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Saturday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(10, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", dtoInconsistente)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", dtoInconsistente);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Confirmar que no se alteró la disponibilidad de vet.carlos
        var guardada = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetCarlosId}");
        Assert.NotNull(guardada);
        Assert.DoesNotContain(guardada, d => d.DiaSemana == DayOfWeek.Saturday);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_RecepcionistaConfigurandoCualquierVeterinarioValido_Retorna200OK(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        var dto = CrearDisponibilidadValidaDto(vetLaura.Id, new TimeSpan(8, 0, 0));

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dto)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<List<DisponibilidadVeterinarioDTO>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_ConRolAdministrador_Retorna403Forbidden_SinModificarDatos(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var dispOriginal = CrearDisponibilidadValidaDto(vetLaura.Id, new TimeSpan(9, 30, 0));
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dispOriginal);

        await LoginAndSetHeaderAsync("admin", "Admin1234!");
        var intentoAdmin = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Thursday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(10, 0, 0)
                }
            }
        };

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoAdmin)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoAdmin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        // Confirmar con Recepcionista que los datos de vet.laura no fueron modificados
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var guardada = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetLaura.Id}");
        Assert.NotNull(guardada);
        Assert.Equal(2, guardada.Count);
        Assert.DoesNotContain(guardada, d => d.DiaSemana == DayOfWeek.Thursday);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_SinToken_Retorna401Unauthorized_SinModificarDatos(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var dispOriginal = CrearDisponibilidadValidaDto(vetLaura.Id, new TimeSpan(9, 0, 0));
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dispOriginal);

        // Quitar token
        _client.DefaultRequestHeaders.Authorization = null;
        var intentoAnonimo = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Sunday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(10, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoAnonimo)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", intentoAnonimo);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        // Confirmar que la disponibilidad sigue intacta
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var guardada = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetLaura.Id}");
        Assert.NotNull(guardada);
        Assert.DoesNotContain(guardada, d => d.DiaSemana == DayOfWeek.Sunday);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_ConTokenSinNameIdentifierValido_Retorna401Unauthorized_SinModificarDatos(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();

        // Generar un token JWT firmado con rol Veterinario pero sin ClaimTypes.NameIdentifier válido
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var secretKey = config["JwtSettings:SecretKey"]!;
        var issuer = config["JwtSettings:Issuer"]!;
        var audience = config["JwtSettings:Audience"]!;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "vet.laura"),
            new Claim(ClaimTypes.Role, "Veterinario"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            // Sin ClaimTypes.NameIdentifier
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        var jwtSinId = new JwtSecurityTokenHandler().WriteToken(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwtSinId);
        var dto = CrearDisponibilidadValidaDto(vetLaura.Id, new TimeSpan(8, 0, 0));

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dto)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_VeterinarioInexistente_ConRolRecepcionista_Retorna404NotFound(string metodoHttp)
    {
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var idInexistente = Guid.NewGuid();
        var dto = CrearDisponibilidadValidaDto(idInexistente, new TimeSpan(8, 0, 0));

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{idInexistente}", dto)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{idInexistente}", dto);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("POST")]
    public async Task ConfigurarDisponibilidad_HorarioInvalido_ConservaValidacionesExistentes_Retorna409Conflict(string metodoHttp)
    {
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dtoInvalido = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(12, 0, 0),
                    HoraFin = new TimeSpan(8, 0, 0) // HoraInicio > HoraFin
                }
            }
        };

        HttpResponseMessage response = metodoHttp == "PUT"
            ? await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dtoInvalido)
            : await _client.PostAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dtoInvalido);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AgendaYOperacionesDeTurnos_ContinuanFuncionandoCorrectamente()
    {
        // 1. Veterinario configura su disponibilidad para todos los días por la mañana
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var disponibilidades = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        var dispRes = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = disponibilidades
        });
        Assert.Equal(HttpStatusCode.OK, dispRes.StatusCode);

        // 2. Recepcionista consulta mascotas, crea un turno, consulta la agenda, reprograma y cancela
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        Assert.NotNull(mascotas);
        var milo = mascotas.First(m => m.Nombre == "Milo" && m.Activa);

        var fechaTurno = DateTime.UtcNow.Date.AddDays(10).AddHours(9);
        var createRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = milo.Id,
            VeterinarioId = vetId,
            FechaHora = fechaTurno,
            MotivoConsulta = "Control clínico general"
        });
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var turnoCreado = await createRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(turnoCreado);

        // Consultar agenda
        var agendaRes = await _client.GetFromJsonAsync<List<TurnoResponseDTO>>($"/api/turnos?veterinarioId={vetId}");
        Assert.NotNull(agendaRes);
        Assert.Contains(agendaRes, t => t.Id == turnoCreado.Id);

        // Reprogramar turno
        var nuevaFecha = fechaTurno.AddMinutes(30);
        var updateRes = await _client.PutAsJsonAsync($"/api/turnos/{turnoCreado.Id}", new TurnoUpdateDTO
        {
            FechaHora = nuevaFecha,
            MotivoConsulta = "Control clínico reprogramado"
        });
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);

        // Cancelar turno
        var cancelRes = await _client.PostAsJsonAsync($"/api/turnos/{turnoCreado.Id}/cancelar", new TurnoCancelarDTO
        {
            MotivoCancelacion = "Cancelación de prueba de integración"
        });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var turnoCancelado = await cancelRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(turnoCancelado);
        Assert.Equal("Cancelado", turnoCancelado.Estado);
    }

    [Theory]
    [InlineData("EstadoInexistente")]
    [InlineData("EnCurso")]
    public async Task GetAgenda_ConEstadoInvalido_Retorna400BadRequest(string estadoInvalido)
    {
        // Arrange (BUG-04 / CU-05 flujo 1a)
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        // Act
        var response = await _client.GetAsync($"/api/turnos?estado={estadoInvalido}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_ConVeterinarioInexistenteOSinRolVeterinario_Retorna404NotFound()
    {
        // Arrange (BUG-04 / CU-05 flujo 3b)
        var recepId = await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        // Act 1: ID inexistente
        var resInexistente = await _client.GetAsync($"/api/turnos?veterinarioId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resInexistente.StatusCode);

        // Act 2: Usuario existente pero con rol Recepcionista (no Veterinario)
        var resNoVet = await _client.GetAsync($"/api/turnos?veterinarioId={recepId}");
        Assert.Equal(HttpStatusCode.NotFound, resNoVet.StatusCode);
    }

    [Fact]
    public async Task GetAgenda_ConFiltroGuidEmpty_Retorna400BadRequest()
    {
        // Arrange (BUG-04: Guid.Empty proporcionado como filtro es inválido)
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        var resVetEmpty = await _client.GetAsync($"/api/turnos?veterinarioId={Guid.Empty}");
        Assert.Equal(HttpStatusCode.BadRequest, resVetEmpty.StatusCode);

        var resMascotaEmpty = await _client.GetAsync($"/api/turnos?mascotaId={Guid.Empty}");
        Assert.Equal(HttpStatusCode.BadRequest, resMascotaEmpty.StatusCode);
    }

    [Fact]
    public async Task Update_TurnoDeMascotaDesactivada_Retorna409Conflict_SinModificarTurno()
    {
        // Arrange (BUG-05 / RN-07 / CU-06 flujo 3e: Impedir reprogramar turnos de mascotas inactivas)
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var disponibilidades = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = disponibilidades
        });

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var duenos = await _client.GetFromJsonAsync<List<Shared.DTOs.Dueno.DuenoResponseDTO>>("/api/duenos");
        var dueno = duenos!.First();

        // Crear mascota temporal activa y agendarle un turno
        var mascotaRes = await _client.PostAsJsonAsync("/api/mascotas", new MascotaCreateDTO
        {
            DuenoId = dueno.Id,
            Nombre = "MascotaBug05",
            Especie = "Perro",
            Raza = "Mestizo",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Peso = 12.0m
        });
        mascotaRes.EnsureSuccessStatusCode();
        var mascota = await mascotaRes.Content.ReadFromJsonAsync<MascotaResponseDTO>();

        var fechaOriginal = DateTime.UtcNow.Date.AddDays(12).AddHours(10);
        var createTurnoRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascota!.Id,
            VeterinarioId = vetId,
            FechaHora = fechaOriginal,
            MotivoConsulta = "Motivo antes de baja"
        });
        Assert.Equal(HttpStatusCode.Created, createTurnoRes.StatusCode);
        var turno = await createTurnoRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();

        // Dar de baja lógica a la mascota
        var desactivarRes = await _client.PatchAsync($"/api/mascotas/{mascota.Id}/desactivar", null);
        Assert.Equal(HttpStatusCode.OK, desactivarRes.StatusCode);

        // Act: Intentar reprogramar el turno de la mascota ahora inactiva
        var nuevaFecha = fechaOriginal.AddMinutes(30);
        var updateRes = await _client.PutAsJsonAsync($"/api/turnos/{turno!.Id}", new TurnoUpdateDTO
        {
            FechaHora = nuevaFecha,
            MotivoConsulta = "Motivo modificado inválido"
        });

        // Assert: 409 Conflict y el turno permanece sin modificaciones
        Assert.Equal(HttpStatusCode.Conflict, updateRes.StatusCode);

        var turnoVerificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turno.Id}");
        Assert.NotNull(turnoVerificado);
        Assert.Equal(fechaOriginal, turnoVerificado.FechaHora);
        Assert.Equal("Motivo antes de baja", turnoVerificado.MotivoConsulta);
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_JornadaConEspaciosYMayusculas_NormalizaYPersisteCorrectamente()
    {
        // Arrange (VAL-08)
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "  mAnAnA  ",
                    HoraInicio = new TimeSpan(8, 30, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                },
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Thursday,
                    Jornada = " TARDE ",
                    HoraInicio = new TimeSpan(14, 0, 0),
                    HoraFin = new TimeSpan(18, 30, 0)
                }
            }
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var guardadas = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetId}");
        Assert.NotNull(guardadas);
        Assert.Equal(2, guardadas.Count);
        Assert.Contains(guardadas, d => d.DiaSemana == DayOfWeek.Monday && d.Jornada == "Manana" && d.HoraInicio == new TimeSpan(8, 30, 0));
        Assert.Contains(guardadas, d => d.DiaSemana == DayOfWeek.Thursday && d.Jornada == "Tarde" && d.HoraFin == new TimeSpan(18, 30, 0));
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_ValidacionesVal08Fallidas_Retornan409Conflict_SinModificarDisponibilidadPrevia()
    {
        // Arrange (VAL-08): Configurar disponibilidad inicial válida
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dispInicial = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Tuesday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(9, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        var initRes = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispInicial);
        Assert.Equal(HttpStatusCode.OK, initRes.StatusCode);

        var casosInvalidos = new List<DisponibilidadVeterinarioDTO>
        {
            // 1. DiaSemana inválido
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = (DayOfWeek)99,
                Jornada = "Manana",
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(12, 0, 0)
            },
            // 2. Jornada fuera de catálogo
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Noche",
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(12, 0, 0)
            },
            // 3. HoraInicio negativa (< 00:00)
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Manana",
                HoraInicio = TimeSpan.FromMinutes(-30),
                HoraFin = new TimeSpan(12, 0, 0)
            },
            // 4. HoraFin mayor a 24:00
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Tarde",
                HoraInicio = new TimeSpan(14, 0, 0),
                HoraFin = TimeSpan.FromHours(25)
            },
            // 5. HoraInicio == HoraFin
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Manana",
                HoraInicio = new TimeSpan(10, 0, 0),
                HoraFin = new TimeSpan(10, 0, 0)
            },
            // 6. Minutos no alineados a bloque de 30m (:15)
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Manana",
                HoraInicio = new TimeSpan(8, 15, 0),
                HoraFin = new TimeSpan(12, 0, 0)
            },
            // 7. Segundos distintos de cero
            new DisponibilidadVeterinarioDTO
            {
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Manana",
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(12, 0, 30)
            }
        };

        foreach (var itemInvalido in casosInvalidos)
        {
            var res = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", new ConfigurarDisponibilidadDTO
            {
                VeterinarioId = vetId,
                Disponibilidades = new List<DisponibilidadVeterinarioDTO> { itemInvalido }
            });
            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        }

        // Caso con elemento nulo en la lista
        var resConElementoNulo = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO> { null! }
        });
        Assert.Equal(HttpStatusCode.Conflict, resConElementoNulo.StatusCode);

        // Verificar que la disponibilidad previa (martes 09:00 a 12:00) permanece intacta
        var guardadas = await _client.GetFromJsonAsync<List<DisponibilidadVeterinarioDTO>>($"/api/turnos/disponibilidad/{vetId}");
        Assert.NotNull(guardadas);
        Assert.Single(guardadas);
        Assert.Equal(DayOfWeek.Tuesday, guardadas[0].DiaSemana);
        Assert.Equal("Manana", guardadas[0].Jornada);
        Assert.Equal(new TimeSpan(9, 0, 0), guardadas[0].HoraInicio);
        Assert.Equal(new TimeSpan(12, 0, 0), guardadas[0].HoraFin);
    }
}
