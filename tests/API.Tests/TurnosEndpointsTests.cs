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
using Shared.DTOs.AtencionMedica;
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

    private async Task<string> ObtenerTokenAsync(string username, string password)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDTO
        {
            UsuarioOEmail = username,
            Password = password
        });
        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<LoginResponseDTO>();
        Assert.NotNull(auth?.Token);
        return auth.Token;
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

    [Fact]
    public async Task Confirmar_ConRolRecepcionista_TurnoPendienteValido_Retorna200OKYEstadoConfirmado()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        // Sembrar un turno en estado Pendiente directamente en BD
        var ahora = DateTime.UtcNow.Date;
        var diasHastaLunes = ((1 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((1 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaLunes = ahora.AddDays(diasHastaLunes).AddHours(9); // 09:00 AM

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var turno = new Turno
            {
                Id = turnoId,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaLunes,
                MotivoConsulta = "Control de seguimiento crónico",
                Estado = "Pendiente",
                EsAutomatico = true,
                TipoOrigen = "SeguimientoCronico"
            };
            db.Turnos.Add(turno);
            await db.SaveChangesAsync();
        }

        // Act: Confirmar como Recepcionista
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/confirmar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmado = await response.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(confirmado);
        Assert.Equal("Confirmado", confirmado.Estado);
    }

    [Fact]
    public async Task Confirmar_ConRolVeterinario_TurnoPendienteValido_Retorna200OKYEstadoConfirmado()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaLunes = ((1 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((1 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaLunes = ahora.AddDays(diasHastaLunes).AddHours(10); // 10:00 AM

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var turno = new Turno
            {
                Id = turnoId,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaLunes,
                MotivoConsulta = "Control de seguimiento crónico",
                Estado = "Pendiente",
                EsAutomatico = true,
                TipoOrigen = "SeguimientoCronico"
            };
            db.Turnos.Add(turno);
            await db.SaveChangesAsync();
        }

        // Act: Confirmar como Veterinario
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/confirmar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var confirmado = await response.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(confirmado);
        Assert.Equal("Confirmado", confirmado.Estado);
    }

    [Fact]
    public async Task Confirmar_ConRolAdministrador_Retorna403Forbidden()
    {
        // Arrange
        await LoginAndSetHeaderAsync("admin", "Admin1234!");
        var turnoId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/confirmar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Confirmar_SinAutenticacion_Retorna401Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;
        var turnoId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/confirmar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirmar_CuandoHorarioYaOcupadoPorTurnoConfirmado_Retorna409Conflict()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaMiercoles = ((3 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((3 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaMiercoles = ahora.AddDays(diasHastaMiercoles).AddHours(9); // 09:00 AM

        var turnoPendienteId = Guid.NewGuid();
        var turnoConfirmadoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoPendienteId,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaMiercoles,
                MotivoConsulta = "Seguimiento pendiente",
                Estado = "Pendiente"
            });
            db.Turnos.Add(new Turno
            {
                Id = turnoConfirmadoId,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaMiercoles,
                MotivoConsulta = "Turno manual confirmado",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Act: Intentar confirmar el turno pendiente en horario ocupado
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var response = await _client.PostAsync($"/api/turnos/{turnoPendienteId}/confirmar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task VariosTurnosPendientesEnMismoHorario_NoBloqueanHorario_HastaQueUnoSeConfirma()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Thursday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaJueves = ((4 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((4 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaJueves = ahora.AddDays(diasHastaJueves).AddHours(11); // 11:00 AM

        var pendiente1Id = Guid.NewGuid();
        var pendiente2Id = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = pendiente1Id,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaJueves,
                MotivoConsulta = "Seguimiento 1",
                Estado = "Pendiente"
            });
            db.Turnos.Add(new Turno
            {
                Id = pendiente2Id,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = fechaJueves,
                MotivoConsulta = "Seguimiento 2",
                Estado = "Pendiente"
            });
            await db.SaveChangesAsync();
        }

        // Act 1: Crear un turno manual en ese mismo horario debe ser exitoso porque los pendientes NO bloquean
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var createManualDto = new TurnoCreateDTO
        {
            MascotaId = mascota.Id,
            VeterinarioId = vetId,
            FechaHora = fechaJueves,
            MotivoConsulta = "Turno manual de urgencia"
        };
        var resManual = await _client.PostAsJsonAsync("/api/turnos", createManualDto);
        Assert.Equal(HttpStatusCode.Created, resManual.StatusCode);

        // Act 2: Al intentar confirmar cualquiera de los pendientes ahora que existe uno Confirmado, se rechaza
        var resConfirmar1 = await _client.PostAsync($"/api/turnos/{pendiente1Id}/confirmar", null);
        Assert.Equal(HttpStatusCode.Conflict, resConfirmar1.StatusCode);
    }

    [Fact]
    public async Task Cancelar_ConMotivoInasistencia_Retorna200OKYRegistraInasistencia()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Friday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaViernes = ((5 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((5 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaViernes = ahora.AddDays(diasHastaViernes).AddHours(9);

        var createRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascota.Id,
            VeterinarioId = vetId,
            FechaHora = fechaViernes,
            MotivoConsulta = "Consulta para control"
        });
        var creado = await createRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(creado);

        // Act: Cancelar con motivo Inasistencia
        var cancelRes = await _client.PostAsJsonAsync($"/api/turnos/{creado.Id}/cancelar", new TurnoCancelarDTO
        {
            MotivoCancelacion = "Inasistencia"
        });

        // Assert
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var cancelado = await cancelRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(cancelado);
        Assert.Equal("Cancelado", cancelado.Estado);
        Assert.Equal("Inasistencia", cancelado.MotivoCancelacion);
        Assert.NotNull(cancelado.FechaCancelacion);
    }

    [Fact]
    public async Task Finalizar_ConRolRecepcionista_Retorna403Forbidden_SinModificarEstado()
    {
        // Arrange: Solo el rol Veterinario está autorizado para finalizar turnos
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaMiercoles = ((3 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((3 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaMiercoles = ahora.AddDays(diasHastaMiercoles).AddHours(10);

        var createRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascota.Id,
            VeterinarioId = vetId,
            FechaHora = fechaMiercoles,
            MotivoConsulta = "Control general"
        });
        var creado = await createRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal("Confirmado", creado.Estado);

        // Act: Intentar finalizar como Recepcionista
        var finalRes = await _client.PostAsync($"/api/turnos/{creado.Id}/finalizar", null);

        // Assert: 403 Forbidden y el estado permanece Confirmado
        Assert.Equal(HttpStatusCode.Forbidden, finalRes.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{creado.Id}");
        Assert.NotNull(verificado);
        Assert.Equal("Confirmado", verificado.Estado);
        Assert.Null(verificado.FechaHoraFinAtencion);
    }

    [Fact]
    public async Task Finalizar_ConRolVeterinarioAsignado_ConAtencionMedicaVinculada_Retorna200OKYEstadoFinalizado()
    {
        // Arrange: Flujo completo Confirmado -> EnAtencion -> AtencionMedica(TurnoId) -> Finalizado
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Wednesday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaMiercoles = ((3 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((3 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaMiercoles = ahora.AddDays(diasHastaMiercoles).AddHours(11);

        var createRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascota.Id,
            VeterinarioId = vetId,
            FechaHora = fechaMiercoles,
            MotivoConsulta = "Consulta clínica"
        });
        var creado = await createRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal("Confirmado", creado.Estado);

        // 1. Iniciar atención
        var iniciarRes = await _client.PostAsync($"/api/turnos/{creado.Id}/iniciar-atencion", null);
        Assert.Equal(HttpStatusCode.OK, iniciarRes.StatusCode);
        var enAtencion = await iniciarRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(enAtencion);
        Assert.Equal("EnAtencion", enAtencion.Estado);
        Assert.NotNull(enAtencion.FechaHoraInicioAtencion);

        // 2. Registrar atención médica vinculada al turno
        var atencionRes = await _client.PostAsJsonAsync("/api/atenciones-medicas", new AtencionMedicaCreateDTO
        {
            MascotaId = mascota.Id,
            TurnoId = creado.Id,
            MotivoConsulta = "Consulta clínica",
            Anamnesis = "Control general sin síntomas",
            ExamenFisico = "Parámetros fisiológicos normales",
            Diagnostico = "Paciente clínicamente sano",
            Tratamiento = "Control anual",
            PesoRegistrado = 15.0m
        });
        Assert.Equal(HttpStatusCode.Created, atencionRes.StatusCode);

        // 3. Act: Finalizar como Veterinario asignado
        var finalRes = await _client.PostAsync($"/api/turnos/{creado.Id}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, finalRes.StatusCode);
        var finalizado = await finalRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(finalizado);
        Assert.Equal("Finalizado", finalizado.Estado);
        Assert.NotNull(finalizado.FechaHoraInicioAtencion);
        Assert.NotNull(finalizado.FechaHoraFinAtencion);
        Assert.True(finalizado.FechaHoraFinAtencion >= finalizado.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task Finalizar_ConRolAdministrador_Retorna403Forbidden()
    {
        // Arrange
        await LoginAndSetHeaderAsync("admin", "Admin1234!");

        // Act
        var response = await _client.PostAsync($"/api/turnos/{Guid.NewGuid()}/finalizar", null);

        // Assert: Administrador no tiene permiso para finalizar turnos clínicos
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Finalizar_SinAutenticacion_Retorna401Unauthorized()
    {
        // Arrange
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.PostAsync($"/api/turnos/{Guid.NewGuid()}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Finalizar_TurnoEnEstadoPendiente_Retorna409Conflict()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascota.Id,
                VeterinarioId = vetId,
                FechaHora = DateTime.UtcNow.AddDays(5),
                MotivoConsulta = "Pendiente crónico",
                Estado = "Pendiente"
            });
            await db.SaveChangesAsync();
        }

        // Act: Intentar finalizar directamente un turno que está Pendiente (no EnAtencion)
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Finalizar_TurnoYaFinalizado_Retorna409Conflict()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dispDto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Friday,
                    Jornada = "Tarde",
                    HoraInicio = new TimeSpan(14, 0, 0),
                    HoraFin = new TimeSpan(18, 0, 0)
                }
            }
        };
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetId}", dispDto);

        var mascotas = await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas");
        var mascota = mascotas!.First(m => m.Activa);

        var ahora = DateTime.UtcNow.Date;
        var diasHastaViernes = ((5 - (int)ahora.DayOfWeek + 7) % 7) == 0 ? 7 : ((5 - (int)ahora.DayOfWeek + 7) % 7);
        var fechaViernes = ahora.AddDays(diasHastaViernes).AddHours(15);

        var createRes = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascota.Id,
            VeterinarioId = vetId,
            FechaHora = fechaViernes,
            MotivoConsulta = "Consulta"
        });
        var creado = await createRes.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(creado);

        // Iniciar atención y vincular atención médica antes del primer finalizado
        var resIniciar = await _client.PostAsync($"/api/turnos/{creado.Id}/iniciar-atencion", null);
        Assert.Equal(HttpStatusCode.OK, resIniciar.StatusCode);

        var resAtencion = await _client.PostAsJsonAsync("/api/atenciones-medicas", new AtencionMedicaCreateDTO
        {
            MascotaId = mascota.Id,
            TurnoId = creado.Id,
            MotivoConsulta = "Consulta",
            Anamnesis = "Control general",
            ExamenFisico = "Examen normal",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 15.0m
        });
        Assert.Equal(HttpStatusCode.Created, resAtencion.StatusCode);

        // Primer finalizado: Exitoso
        var res1 = await _client.PostAsync($"/api/turnos/{creado.Id}/finalizar", null);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Act: Segundo intento de finalización sobre un turno ya finalizado
        var res2 = await _client.PostAsync($"/api/turnos/{creado.Id}/finalizar", null);

        // Assert: Debe ser rechazado con conflicto
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Finalizar_TurnoInexistente_Retorna404NotFound()
    {
        // Arrange
        await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        // Act
        var response = await _client.PostAsync($"/api/turnos/{Guid.NewGuid()}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // =========================================================================
    // ETAPA 4: Pruebas de integración HTTP para IniciarAtencion, Finalizar y EnAtencion
    // =========================================================================

    [Fact]
    public async Task IniciarAtencion_VeterinarioAsignado_TurnoConfirmado_Retorna200OKYRegistraFechaHoraInicioAtencion()
    {
        // Arrange
        var vetId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetId,
                FechaHora = DateTime.UtcNow.AddDays(6).Date.AddHours(9),
                MotivoConsulta = "Control dermatológico",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        var antesUtc = DateTime.UtcNow.AddSeconds(-2);

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/iniciar-atencion", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var actualizado = await response.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(actualizado);
        Assert.Equal("EnAtencion", actualizado.Estado);
        Assert.NotNull(actualizado.FechaHoraInicioAtencion);
        Assert.True(actualizado.FechaHoraInicioAtencion.Value >= antesUtc);
        Assert.Null(actualizado.FechaHoraFinAtencion);

        // Verificar persistencia real en BD
        var getTurno = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(getTurno);
        Assert.Equal("EnAtencion", getTurno.Estado);
        Assert.NotNull(getTurno.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task IniciarAtencion_PorOtroVeterinarioDistintoAlAsignado_Retorna403Forbidden_SinModificarTurno()
    {
        // Arrange
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(7).Date.AddHours(10),
                MotivoConsulta = "Consulta asignada a Carlos",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Autenticar como vet.laura (otro veterinario)
        await LoginAndSetHeaderAsync("vet.laura", "VetLaura1234!");

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/iniciar-atencion", null);

        // Assert: 403 Forbidden y estado permanece Confirmado
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("Confirmado", verificado.Estado);
        Assert.Null(verificado.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task IniciarAtencion_ConRolRecepcionista_Retorna403Forbidden_SinModificarTurno()
    {
        // Arrange
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(8).Date.AddHours(10),
                MotivoConsulta = "Consulta clínica",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Autenticar como Recepcionista
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/iniciar-atencion", null);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("Confirmado", verificado.Estado);
        Assert.Null(verificado.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task IniciarAtencion_SinAutenticacionYConRolAdministrador_Retorna401Y403()
    {
        // 1. Sin autenticación -> 401 Unauthorized
        _client.DefaultRequestHeaders.Authorization = null;
        var resAnon = await _client.PostAsync($"/api/turnos/{Guid.NewGuid()}/iniciar-atencion", null);
        Assert.Equal(HttpStatusCode.Unauthorized, resAnon.StatusCode);

        // 2. Con rol Administrador -> 403 Forbidden
        await LoginAndSetHeaderAsync("admin", "Admin1234!");
        var resAdmin = await _client.PostAsync($"/api/turnos/{Guid.NewGuid()}/iniciar-atencion", null);
        Assert.Equal(HttpStatusCode.Forbidden, resAdmin.StatusCode);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("EnAtencion")]
    [InlineData("Cancelado")]
    [InlineData("Finalizado")]
    public async Task IniciarAtencion_DesdeEstadoNoConfirmado_Retorna409Conflict_SinAlterarEstado(string estadoInicial)
    {
        // Arrange
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        var inicioPrevio = estadoInicial == "EnAtencion" ? DateTime.UtcNow.AddMinutes(-10) : (DateTime?)null;
        var horaSegunEstado = estadoInicial switch
        {
            "Pendiente" => 14,
            "EnAtencion" => 15,
            "Cancelado" => 16,
            "Finalizado" => 17,
            _ => 18
        };

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(9).Date.AddHours(horaSegunEstado),
                MotivoConsulta = "Prueba de estado inválido para iniciar",
                Estado = estadoInicial,
                FechaHoraInicioAtencion = inicioPrevio
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/iniciar-atencion", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal(estadoInicial, verificado.Estado);
    }

    [Fact]
    public async Task Finalizar_TurnoEnAtencionSinAtencionMedicaVinculada_Retorna409Conflict_SinModificarEstado()
    {
        // Arrange
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        var inicioAtencion = DateTime.UtcNow.AddMinutes(-15);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(9).Date.AddHours(11),
                MotivoConsulta = "Turno en atención sin registro médico",
                Estado = "EnAtencion",
                FechaHoraInicioAtencion = inicioAtencion
            });
            await db.SaveChangesAsync();
        }

        // Act: Intentar finalizar sin haber registrado atención médica con TurnoId
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/finalizar", null);

        // Assert: 409 Conflict y el turno permanece EnAtencion sin FechaHoraFinAtencion
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("EnAtencion", verificado.Estado);
        Assert.Null(verificado.FechaHoraFinAtencion);
    }

    [Fact]
    public async Task Finalizar_TurnoConfirmadoSinIniciarAtencion_Retorna409Conflict_SinModificarEstado()
    {
        // Arrange: Ya no se permite Confirmado -> Finalizado directo
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(10).Date.AddHours(11),
                MotivoConsulta = "Turno confirmado no iniciado",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync($"/api/turnos/{turnoId}/finalizar", null);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("Confirmado", verificado.Estado);
        Assert.Null(verificado.FechaHoraFinAtencion);
    }

    [Fact]
    public async Task Finalizar_PorOtroVeterinarioDistintoAlAsignado_Retorna403Forbidden_SinModificarEstado()
    {
        // Arrange
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = DateTime.UtcNow.AddDays(11).Date.AddHours(9),
                MotivoConsulta = "Consulta asignada a Carlos",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Carlos inicia la atención y registra la atención médica
        var resInit = await _client.PostAsync($"/api/turnos/{turnoId}/iniciar-atencion", null);
        Assert.Equal(HttpStatusCode.OK, resInit.StatusCode);

        var resAtencion = await _client.PostAsJsonAsync("/api/atenciones-medicas", new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            TurnoId = turnoId,
            MotivoConsulta = "Consulta",
            Anamnesis = "Anamnesis",
            ExamenFisico = "Examen físico",
            Diagnostico = "Diagnóstico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 14.0m
        });
        Assert.Equal(HttpStatusCode.Created, resAtencion.StatusCode);

        // Ahora vet.laura intenta finalizar el turno de vet.carlos
        await LoginAndSetHeaderAsync("vet.laura", "VetLaura1234!");
        var resFinalizarIntruso = await _client.PostAsync($"/api/turnos/{turnoId}/finalizar", null);

        // Assert: 403 Forbidden y sigue EnAtencion
        Assert.Equal(HttpStatusCode.Forbidden, resFinalizarIntruso.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("EnAtencion", verificado.Estado);
        Assert.Null(verificado.FechaHoraFinAtencion);
    }

    [Fact]
    public async Task ModificarOCancelar_TurnoEnAtencion_Retorna409Conflict_SinAlterarDatos()
    {
        // Arrange
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var fechaOriginal = DateTime.UtcNow.AddDays(14).Date.AddHours(10);
        var inicioOriginal = DateTime.UtcNow.AddMinutes(-5);
        var turnoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = fechaOriginal,
                MotivoConsulta = "Motivo original en curso",
                Estado = "EnAtencion",
                FechaHoraInicioAtencion = inicioOriginal
            });
            await db.SaveChangesAsync();
        }

        // Act 1: Intentar reprogramar (PUT /api/turnos/{id})
        var resUpdate = await _client.PutAsJsonAsync($"/api/turnos/{turnoId}", new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddHours(1),
            MotivoConsulta = "Motivo alterado"
        });
        Assert.Equal(HttpStatusCode.Conflict, resUpdate.StatusCode);

        // Act 2: Intentar cancelar por POST (/api/turnos/{id}/cancelar)
        var resCancelPost = await _client.PostAsJsonAsync($"/api/turnos/{turnoId}/cancelar", new TurnoCancelarDTO
        {
            MotivoCancelacion = "Intento de cancelación durante atención"
        });
        Assert.Equal(HttpStatusCode.Conflict, resCancelPost.StatusCode);

        // Act 3: Intentar cancelar por PATCH (/api/turnos/{id}/cancelar)
        var resCancelPatch = await _client.PatchAsJsonAsync($"/api/turnos/{turnoId}/cancelar", new TurnoCancelarDTO
        {
            MotivoCancelacion = "Intento de cancelación PATCH durante atención"
        });
        Assert.Equal(HttpStatusCode.Conflict, resCancelPatch.StatusCode);

        // Assert: El turno conserva intactos su Estado, FechaHora y MotivoConsulta
        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("EnAtencion", verificado.Estado);
        Assert.Equal(fechaOriginal, verificado.FechaHora);
        Assert.Equal("Motivo original en curso", verificado.MotivoConsulta);
        Assert.Null(verificado.MotivoCancelacion);
        Assert.Null(verificado.FechaCancelacion);
    }

    // =========================================================================
    // AUTORIZACIÓN HORIZONTAL DE TURNOS: Create, Confirmar, Update y Cancelar
    // =========================================================================

    [Fact]
    public async Task Create_VeterinarioIntentaCrearTurnoParaOtroVeterinario_Retorna403Forbidden_SinCrearTurno()
    {
        // Arrange
        var vetLaura = await EnsureSecondVeterinarioAsync();
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");

        var dispLaura = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = dispLaura
        });

        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var fechaTurno = DateTime.UtcNow.Date.AddDays(21).AddHours(9);

        // Autenticar como vet.carlos e intentar agendar turno para vet.laura
        await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var res = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetLaura.Id,
            FechaHora = fechaTurno,
            MotivoConsulta = "Intento de agendar para otro colega"
        });

        // Assert: 403 Forbidden y no se creó ningún turno en ese horario
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var agendaLaura = await _client.GetFromJsonAsync<List<TurnoResponseDTO>>($"/api/turnos?veterinarioId={vetLaura.Id}");
        Assert.NotNull(agendaLaura);
        Assert.DoesNotContain(agendaLaura, t => t.FechaHora == fechaTurno);
    }

    [Fact]
    public async Task Create_VeterinarioCreaTurnoAsignadoASiMismo_Retorna201Created()
    {
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dispCarlos = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = dispCarlos
        });

        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var fechaTurno = DateTime.UtcNow.Date.AddDays(22).AddHours(9).AddMinutes(30);

        var res = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetCarlosId,
            FechaHora = fechaTurno,
            MotivoConsulta = "Turno propio agendado por el veterinario"
        });

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var creado = await res.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(creado);
        Assert.Equal(vetCarlosId, creado.VeterinarioId);
        Assert.Equal("Confirmado", creado.Estado);
    }

    [Fact]
    public async Task Confirmar_VeterinarioIntentaConfirmarTurnoDeOtroVeterinario_Retorna403Forbidden_SinModificarEstado()
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        var fechaTurno = DateTime.UtcNow.Date.AddDays(23).AddHours(10);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = fechaTurno,
                MotivoConsulta = "Control asignado a Carlos",
                Estado = "Pendiente"
            });
            await db.SaveChangesAsync();
        }

        // vet.laura intenta confirmar el turno pendiente de vet.carlos
        await LoginAndSetHeaderAsync("vet.laura", "VetLaura1234!");
        var res = await _client.PostAsync($"/api/turnos/{turnoId}/confirmar", null);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);

        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("Pendiente", verificado.Estado);
    }

    [Fact]
    public async Task Update_VeterinarioIntentaModificarTurnoAjenoOReasignarSuPropioTurno_Retorna403Forbidden_SinModificarDatos()
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var disp = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = disp
        });

        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetLaura.Id}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetLaura.Id,
            Disponibilidades = disp
        });

        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var fechaOriginal = DateTime.UtcNow.Date.AddDays(24).AddHours(9);
        var turnoId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = fechaOriginal,
                MotivoConsulta = "Motivo original de Carlos",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // Caso 1: vet.laura intenta modificar el turno de vet.carlos -> 403 Forbidden
        await LoginAndSetHeaderAsync("vet.laura", "VetLaura1234!");
        var resAjeno = await _client.PutAsJsonAsync($"/api/turnos/{turnoId}", new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddMinutes(30),
            MotivoConsulta = "Modificación no autorizada por otro veterinario"
        });
        Assert.Equal(HttpStatusCode.Forbidden, resAjeno.StatusCode);

        // Caso 2: vet.carlos intenta reasignar su propio turno a vet.laura -> 403 Forbidden
        await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var resReasignar = await _client.PutAsJsonAsync($"/api/turnos/{turnoId}", new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddMinutes(30),
            VeterinarioId = vetLaura.Id,
            MotivoConsulta = "Intento de reasignar a otro veterinario"
        });
        Assert.Equal(HttpStatusCode.Forbidden, resReasignar.StatusCode);

        // Verificar que los datos originales permanecen intactos
        var intacto = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(intacto);
        Assert.Equal(vetCarlosId, intacto.VeterinarioId);
        Assert.Equal(fechaOriginal, intacto.FechaHora);
        Assert.Equal("Motivo original de Carlos", intacto.MotivoConsulta);

        // Caso 3: vet.carlos modifica su propio turno sin cambiar VeterinarioId -> 200 OK
        var nuevaFechaPropia = fechaOriginal.AddMinutes(30);
        var resPropio = await _client.PutAsJsonAsync($"/api/turnos/{turnoId}", new TurnoUpdateDTO
        {
            FechaHora = nuevaFechaPropia,
            MotivoConsulta = "Reprogramado por el propio veterinario"
        });
        Assert.Equal(HttpStatusCode.OK, resPropio.StatusCode);

        // Caso 4: Recepcionista reasigna el turno de vet.carlos a vet.laura -> 200 OK
        await LoginAndSetHeaderAsync("recep.ana", "Recep1234!");
        var resRecep = await _client.PutAsJsonAsync($"/api/turnos/{turnoId}", new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddHours(1),
            VeterinarioId = vetLaura.Id,
            MotivoConsulta = "Reasignado legítimamente por recepción"
        });
        Assert.Equal(HttpStatusCode.OK, resRecep.StatusCode);
        var actualizadoPorRecep = await resRecep.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(actualizadoPorRecep);
        Assert.Equal(vetLaura.Id, actualizadoPorRecep.VeterinarioId);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PATCH")]
    public async Task Cancelar_VeterinarioIntentaCancelarTurnoDeOtroVeterinario_Retorna403Forbidden_SinModificarEstado(string metodoHttp)
    {
        var vetLaura = await EnsureSecondVeterinarioAsync();
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;

        var turnoId = Guid.NewGuid();
        var fechaTurno = DateTime.UtcNow.Date.AddDays(25).AddHours(10);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.Turnos.Add(new Turno
            {
                Id = turnoId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                FechaHora = fechaTurno,
                MotivoConsulta = "Turno asignado a Carlos",
                Estado = "Confirmado"
            });
            await db.SaveChangesAsync();
        }

        // vet.laura intenta cancelar el turno de vet.carlos
        await LoginAndSetHeaderAsync("vet.laura", "VetLaura1234!");
        var dtoCancelar = new TurnoCancelarDTO
        {
            MotivoCancelacion = "Intento de cancelación por otro veterinario"
        };

        HttpResponseMessage resAjeno = metodoHttp == "POST"
            ? await _client.PostAsJsonAsync($"/api/turnos/{turnoId}/cancelar", dtoCancelar)
            : await _client.PatchAsJsonAsync($"/api/turnos/{turnoId}/cancelar", dtoCancelar);

        Assert.Equal(HttpStatusCode.Forbidden, resAjeno.StatusCode);

        // El turno continúa Confirmado y sin datos de cancelación
        var verificado = await _client.GetFromJsonAsync<TurnoResponseDTO>($"/api/turnos/{turnoId}");
        Assert.NotNull(verificado);
        Assert.Equal("Confirmado", verificado.Estado);
        Assert.Null(verificado.MotivoCancelacion);
        Assert.Null(verificado.FechaCancelacion);

        // Ahora el propio vet.carlos cancela su turno -> 200 OK
        await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var resPropio = await _client.PostAsJsonAsync($"/api/turnos/{turnoId}/cancelar", new TurnoCancelarDTO
        {
            MotivoCancelacion = "Cancelado por el propio veterinario"
        });
        Assert.Equal(HttpStatusCode.OK, resPropio.StatusCode);
        var cancelado = await resPropio.Content.ReadFromJsonAsync<TurnoResponseDTO>();
        Assert.NotNull(cancelado);
        Assert.Equal("Cancelado", cancelado.Estado);
    }

    [Fact]
    public async Task Concurrencia_ReservasYConfirmacionesSimultaneasMismoHorario_UnoSoloTieneExitoSinDuplicadosNiErrores500()
    {
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var tokenRecep = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var dispCompleta = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = dispCompleta
        });

        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var fechaObjetivo = DateTime.UtcNow.Date.AddDays(28).AddHours(10);

        // Sembrar 3 turnos Pendiente en el mismo horario para competir con 3 creaciones manuales simultáneas
        var turnosPendientesIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            foreach (var tId in turnosPendientesIds)
            {
                db.Turnos.Add(new Turno
                {
                    Id = tId,
                    MascotaId = mascotaId,
                    VeterinarioId = vetCarlosId,
                    FechaHora = fechaObjetivo,
                    MotivoConsulta = "Turno pendiente concurrente",
                    Estado = "Pendiente",
                    EsAutomatico = true,
                    TipoOrigen = "SeguimientoCronico",
                    FechaCreacion = DateTime.UtcNow
                });
            }
            await db.SaveChangesAsync();
        }

        // Preparar 6 tareas concurrentes (3 POST /api/turnos + 3 POST /api/turnos/{id}/confirmar)
        var tareas = new List<Task<HttpResponseMessage>>();

        for (int i = 0; i < 3; i++)
        {
            tareas.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecep);
                return await client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
                {
                    MascotaId = mascotaId,
                    VeterinarioId = vetCarlosId,
                    FechaHora = fechaObjetivo,
                    MotivoConsulta = $"Reserva manual concurrente {i}"
                });
            }));
        }

        foreach (var pendienteId in turnosPendientesIds)
        {
            tareas.Add(Task.Run(async () =>
            {
                using var client = _factory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecep);
                return await client.PostAsync($"/api/turnos/{pendienteId}/confirmar", null);
            }));
        }

        var respuestas = await Task.WhenAll(tareas);

        // Ninguna respuesta debe ser 500 Internal Server Error
        Assert.DoesNotContain(respuestas, r => (int)r.StatusCode >= 500);

        // Exactamente 1 operación debe ganar el horario (201 Created o 200 OK) y las 5 restantes recibir 409 Conflict
        var exitosas = respuestas.Count(r => r.StatusCode == HttpStatusCode.Created || r.StatusCode == HttpStatusCode.OK);
        var conflictos = respuestas.Count(r => r.StatusCode == HttpStatusCode.Conflict);

        Assert.Equal(1, exitosas);
        Assert.Equal(5, conflictos);

        // Verificar en SQLite que existe exactamente 1 turno Confirmado en ese horario
        using (var scopeCheck = _factory.Services.CreateScope())
        {
            var db = scopeCheck.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var confirmadosEnSlot = await db.Turnos.CountAsync(t =>
                t.VeterinarioId == vetCarlosId &&
                t.FechaHora == fechaObjetivo &&
                (t.Estado == "Confirmado" || t.Estado == "EnAtencion" || t.Estado == "Finalizado"));

            Assert.Equal(1, confirmadosEnSlot);
        }
    }

    [Fact]
    public async Task Concurrencia_GeneracionSeguimientoCronicoMismaAtencion_UnaSolaSeriePersistidaSinDuplicados()
    {
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var dispCompleta = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = dispCompleta
        });

        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var atencionId = Guid.NewGuid();

        using (var scopeSeed = _factory.Services.CreateScope())
        {
            var db = scopeSeed.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            db.AtencionesMedicas.Add(new AtencionMedica
            {
                Id = atencionId,
                MascotaId = mascotaId,
                VeterinarioId = vetCarlosId,
                Fecha = DateTime.UtcNow,
                MotivoConsulta = "Control crónico concurrente",
                Anamnesis = "Anamnesis de prueba",
                ExamenFisico = "Examen físico normal",
                Diagnostico = "Hipotiroidismo canino",
                Tratamiento = "Levotiroxina",
                PesoRegistrado = 18.0m,
                EsAptoVacunacion = true
            });
            await db.SaveChangesAsync();
        }

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetCarlosId,
            AtencionMedicaId = atencionId,
            FechaAtencion = DateTime.UtcNow.Date.AddDays(30),
            Frecuencia = "Mensual",
            JornadaPreferida = "Manana"
        };

        // Lanzar 4 generaciones concurrentes en scopes independientes sobre SQLite para la misma AtencionMedicaId
        var tareas = Enumerable.Range(0, 4).Select(_ => Task.Run(async () =>
        {
            using var scope = _factory.Services.CreateScope();
            var turnoService = scope.ServiceProvider.GetRequiredService<BusinessLogic.Interfaces.ITurnoService>();
            return await turnoService.GenerarTurnosSeguimientoCronicoAsync(dto);
        })).ToArray();

        var resultados = await Task.WhenAll(
            tareas.Select(t => t.ContinueWith(task => (
                Exito: task.IsCompletedSuccessfully,
                Excepcion: task.Exception?.InnerException
            )))
        );

        Assert.Equal(1, resultados.Count(r => r.Exito));
        Assert.Equal(3, resultados.Count(r => r.Excepcion is Shared.Exceptions.SeguimientoCronicoDuplicadoException));

        // Verificar en SQLite que solo se persistió 1 serie (6 turnos mensuales) y no hay duplicados
        using (var scopeCheck = _factory.Services.CreateScope())
        {
            var db = scopeCheck.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var turnosCronicos = await db.Turnos
                .Where(t => t.AtencionMedicaId == atencionId)
                .ToListAsync();

            Assert.Equal(6, turnosCronicos.Count);
            Assert.Equal(6, turnosCronicos.Select(t => t.FechaHora).Distinct().Count());
        }
    }

    [Fact]
    public async Task Concurrencia_ReemplazosSimultaneosDisponibilidadMismoVeterinario_SinErrores500NiDuplicados()
    {
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");
        var tokenRecep = await ObtenerTokenAsync("recep.ana", "Recep1234!");

        var configA = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = new TimeSpan(8, 0, 0), HoraFin = new TimeSpan(12, 0, 0) },
                new() { DiaSemana = DayOfWeek.Wednesday, Jornada = "Manana", HoraInicio = new TimeSpan(8, 0, 0), HoraFin = new TimeSpan(12, 0, 0) }
            }
        };

        var configB = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Tuesday, Jornada = "Tarde", HoraInicio = new TimeSpan(14, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                new() { DiaSemana = DayOfWeek.Thursday, Jornada = "Tarde", HoraInicio = new TimeSpan(14, 0, 0), HoraFin = new TimeSpan(18, 0, 0) },
                new() { DiaSemana = DayOfWeek.Friday, Jornada = "Tarde", HoraInicio = new TimeSpan(14, 0, 0), HoraFin = new TimeSpan(18, 0, 0) }
            }
        };

        // Ejecutar 6 reemplazos simultáneos (3 con configA y 3 con configB)
        var tareas = Enumerable.Range(0, 6).Select(i => Task.Run(async () =>
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenRecep);
            var payload = i % 2 == 0 ? configA : configB;
            return await client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", payload);
        })).ToArray();

        var respuestas = await Task.WhenAll(tareas);

        // Ninguna solicitud debe arrojar 500 Internal Server Error (DbUpdateConcurrencyException)
        Assert.All(respuestas, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

        // Verificar que la base de datos SQLite haya quedado en un estado consistente (exactamente configA o configB, sin mezcla ni duplicados)
        using (var scopeCheck = _factory.Services.CreateScope())
        {
            var db = scopeCheck.ServiceProvider.GetRequiredService<VeterinariaDbContext>();
            var vigentes = await db.DisponibilidadesVeterinarios
                .Where(d => d.VeterinarioId == vetCarlosId && d.Activo)
                .ToListAsync();

            Assert.True(vigentes.Count == 2 || vigentes.Count == 3);
            Assert.Equal(vigentes.Count, vigentes.Select(d => (d.DiaSemana, d.Jornada)).Distinct().Count());
        }

        // Restaurar disponibilidad completa de lunes a domingo para no afectar pruebas posteriores y verificar regla RN-10
        var dispRestaurada = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinarioDTO
        {
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0)
        }).ToList();

        var resRestaurar = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = dispRestaurada
        });
        Assert.Equal(HttpStatusCode.OK, resRestaurar.StatusCode);

        // Verificar conservación de regla RN-10: intento de reservar fuera de horario (19:00) devuelve 409 Conflict
        var mascotaId = (await _client.GetFromJsonAsync<List<MascotaResponseDTO>>("/api/mascotas"))!.First(m => m.Activa).Id;
        var resFueraHorario = await _client.PostAsJsonAsync("/api/turnos", new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetCarlosId,
            FechaHora = DateTime.UtcNow.Date.AddDays(29).AddHours(19),
            MotivoConsulta = "Fuera de horario tras reemplazo concurrente"
        });
        Assert.Equal(HttpStatusCode.Conflict, resFueraHorario.StatusCode);
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_ConDiaYJornadaDuplicados_Retorna409Conflict()
    {
        var vetCarlosId = await LoginAndSetHeaderAsync("vet.carlos", "Vet1234!");

        var dtoDuplicado = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetCarlosId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = new TimeSpan(8, 0, 0), HoraFin = new TimeSpan(10, 0, 0) },
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = new TimeSpan(10, 0, 0), HoraFin = new TimeSpan(12, 0, 0) }
            }
        };

        var response = await _client.PutAsJsonAsync($"/api/turnos/disponibilidad/{vetCarlosId}", dtoDuplicado);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
