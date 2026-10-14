using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.Turno;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class TurnoServiceTests
{
    private readonly Mock<ITurnoRepository> _turnoRepoMock;
    private readonly Mock<IDisponibilidadVeterinarioRepository> _disponibilidadRepoMock;
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly TurnoService _service;

    public TurnoServiceTests()
    {
        _turnoRepoMock = new Mock<ITurnoRepository>();
        _disponibilidadRepoMock = new Mock<IDisponibilidadVeterinarioRepository>();
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();

        _service = new TurnoService(
            _turnoRepoMock.Object,
            _disponibilidadRepoMock.Object,
            _mascotaRepoMock.Object,
            _usuarioRepoMock.Object);
    }

    // --- ALTA MANUAL (CU-06 Flujo Principal y Alternativos) ---

    [Fact]
    public async Task CreateManualAsync_MascotaInexistente_LanzaMascotaNotFoundException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            MotivoConsulta = "Consulta general"
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync((Mascota?)null);

        await Assert.ThrowsAsync<MascotaNotFoundException>(() => _service.CreateManualAsync(dto));
        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task CreateManualAsync_MascotaInactiva_LanzaMascotaInactivaException()
    {
        // RN-07
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            MotivoConsulta = "Consulta general"
        };

        var mascotaInactiva = new Mascota { Id = mascotaId, Nombre = "Rocky", Activa = false };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascotaInactiva);

        await Assert.ThrowsAsync<MascotaInactivaException>(() => _service.CreateManualAsync(dto));
        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task CreateManualAsync_VeterinarioInexistenteOInactivo_LanzaVeterinarioNotFoundException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(9),
            MotivoConsulta = "Consulta general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync((Usuario?)null);

        await Assert.ThrowsAsync<VeterinarioNotFoundException>(() => _service.CreateManualAsync(dto));
    }

    [Fact]
    public async Task CreateManualAsync_HorarioNoAlineadoABloqueDe30Minutos_LanzaValidationException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaDesalineada = DateTime.UtcNow.AddDays(1).Date.AddHours(9).AddMinutes(15); // :15 no permitido
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaDesalineada,
            MotivoConsulta = "Consulta general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateManualAsync(dto));
    }

    [Fact]
    public async Task CreateManualAsync_FechaEnElPasado_LanzaValidationException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaPasada = DateTime.UtcNow.AddDays(-1).Date.AddHours(10);
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaPasada,
            MotivoConsulta = "Consulta general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateManualAsync(dto));
    }

    [Fact]
    public async Task CreateManualAsync_FueraDeDisponibilidadConfigurada_LanzaHorarioNoDisponibleException()
    {
        // RN-10
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            MotivoConsulta = "Consulta general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(false);

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => _service.CreateManualAsync(dto));
        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task CreateManualAsync_SuperposicionDeHorario_LanzaHorarioNoDisponibleException()
    {
        // RN-08
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            MotivoConsulta = "Consulta general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fechaFutura, null)).ReturnsAsync(true);

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => _service.CreateManualAsync(dto));
        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task CreateManualAsync_DatosValidos_CreaTurnoConfirmadoYOrigenManual()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9).AddMinutes(30);
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            MotivoConsulta = "Control anual"
        };

        var dueno = new Dueno { Id = Guid.NewGuid(), Nombre = "Juan", Apellido = "Pérez" };
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, DuenoId = dueno.Id, Dueno = dueno };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fechaFutura, null)).ReturnsAsync(false);

        Turno? turnoPersistido = null;
        _turnoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Turno>()))
            .Callback<Turno>(t =>
            {
                turnoPersistido = t;
                t.Mascota = mascota;
                t.Veterinario = vet;
            })
            .ReturnsAsync((Turno t) => t);

        _turnoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() => turnoPersistido);

        // Act
        var result = await _service.CreateManualAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Confirmado", result.Estado);
        Assert.Equal("Manual", result.TipoOrigen);
        Assert.False(result.EsAutomatico);
        Assert.Equal(dto.MotivoConsulta, result.MotivoConsulta);
        Assert.Equal("Milo", result.MascotaNombre);
        Assert.Equal("Juan Pérez", result.DuenoNombreCompleto);
        Assert.Equal("vet.carlos", result.VeterinarioNombreCompleto);
    }

    // --- REPROGRAMACIÓN / MODIFICACIÓN ---

    [Fact]
    public async Task UpdateAsync_TurnoInexistente_LanzaTurnoNotFoundException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoUpdateDTO { FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(10) };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync((Turno?)null);

        await Assert.ThrowsAsync<TurnoNotFoundException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_TurnoFinalizado_LanzaTurnoFinalizadoException()
    {
        // RN-09
        var id = Guid.NewGuid();
        var dto = new TurnoUpdateDTO { FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(10) };
        var turno = new Turno { Id = id, Estado = "Finalizado" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoFinalizadoException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_TurnoCancelado_LanzaTurnoYaCanceladoException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoUpdateDTO { FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(10) };
        var turno = new Turno { Id = id, Estado = "Cancelado" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoYaCanceladoException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_SuperposicionConOtroTurno_LanzaHorarioNoDisponibleException()
    {
        var id = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);
        var dto = new TurnoUpdateDTO { FechaHora = nuevaFecha };

        var turno = new Turno { Id = id, VeterinarioId = vetId, Estado = "Confirmado" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, nuevaFecha, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, nuevaFecha, id)).ReturnsAsync(true);

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => _service.UpdateAsync(id, dto));
    }

    // --- CANCELACIÓN INDIVIDUAL CON AUDITORÍA Y LIBERACIÓN (RN-08, RN-09) ---

    [Fact]
    public async Task CancelarAsync_MotivoVacio_LanzaValidationException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoCancelarDTO { MotivoCancelacion = "   " };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.CancelarAsync(id, dto, Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task CancelarAsync_TurnoFinalizado_LanzaTurnoFinalizadoException()
    {
        // RN-09
        var id = Guid.NewGuid();
        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Paciente ausente" };
        var turno = new Turno { Id = id, Estado = "Finalizado" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoFinalizadoException>(() =>
            _service.CancelarAsync(id, dto, Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task CancelarAsync_TurnoYaCancelado_LanzaTurnoYaCanceladoException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Repetición" };
        var turno = new Turno { Id = id, Estado = "Cancelado" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoYaCanceladoException>(() =>
            _service.CancelarAsync(id, dto, Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task CancelarAsync_DatosValidos_CancelaConAuditoriaYLiberaHorario()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var username = "recep.ana";
        var motivo = "Dueño cancela por viaje de trabajo";
        var dto = new TurnoCancelarDTO { MotivoCancelacion = motivo };

        var turno = new Turno
        {
            Id = id,
            Estado = "Confirmado",
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(11),
            MotivoConsulta = "Consulta"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.UpdateAsync(turno)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(turno);

        // Act
        var result = await _service.CancelarAsync(id, dto, userId, username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cancelado", turno.Estado);
        Assert.Equal(motivo, turno.MotivoCancelacion);
        Assert.Equal(userId, turno.UsuarioCancelacionId);
        Assert.Equal(username, turno.UsuarioCancelacionUsername);
        Assert.NotNull(turno.FechaCancelacion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(turno), Times.Once);
    }

    // --- DISPONIBILIDAD SEMANAL DE VETERINARIOS ---

    [Fact]
    public async Task ConfigurarDisponibilidad_HoraInicioMayorOIgualFin_LanzaDisponibilidadInvalidaException()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        var dto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(12, 0, 0),
                    HoraFin = new TimeSpan(8, 0, 0) // Invertido
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_SuperposicionMismoDia_LanzaDisponibilidadInvalidaException()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        var dto = new ConfigurarDisponibilidadDTO
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
                },
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Tarde",
                    HoraInicio = new TimeSpan(11, 0, 0), // Superpuesto con 8-12
                    HoraFin = new TimeSpan(15, 0, 0)
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));
    }

    // --- SEGUIMIENTOS DE ENFERMEDADES CRÓNICAS (RN-11) ---

    [Fact]
    public async Task GenerarTurnosSeguimientoCronico_MascotaInactiva_LanzaMascotaInactivaException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaAtencion = DateTime.UtcNow,
            Frecuencia = "Mensual",
            JornadaPreferida = "Manana"
        };

        var mascota = new Mascota { Id = mascotaId, Activa = false };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        await Assert.ThrowsAsync<MascotaInactivaException>(() => _service.GenerarTurnosSeguimientoCronicoAsync(dto));
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronico_AtencionDuplicada_LanzaSeguimientoCronicoDuplicadoException()
    {
        // RN-11
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var atencionId = Guid.NewGuid();

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            AtencionMedicaId = atencionId,
            FechaAtencion = DateTime.UtcNow,
            Frecuencia = "Mensual",
            JornadaPreferida = "Manana"
        };

        var mascota = new Mascota { Id = mascotaId, Activa = true };
        var vet = new Usuario { Id = vetId, Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.ExisteSeguimientoParaAtencionAsync(atencionId)).ReturnsAsync(true);

        await Assert.ThrowsAsync<SeguimientoCronicoDuplicadoException>(() => _service.GenerarTurnosSeguimientoCronicoAsync(dto));
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronico_Mensual_GeneraTurnosPendientesConHorizonte6Meses()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var atencionId = Guid.NewGuid();
        var fechaAtencion = DateTime.UtcNow.Date;

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            AtencionMedicaId = atencionId,
            FechaAtencion = fechaAtencion,
            Frecuencia = "Mensual",
            JornadaPreferida = "Manana"
        };

        var dueno = new Dueno { Id = Guid.NewGuid(), Nombre = "Mariana", Apellido = "López" };
        var mascota = new Mascota { Id = mascotaId, Nombre = "Luna", Activa = true, Dueno = dueno };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.ExisteSeguimientoParaAtencionAsync(atencionId)).ReturnsAsync(false);

        // Disponibilidad todos los días por la mañana de 8 a 12
        var disponibilidades = Enum.GetValues<DayOfWeek>().Select(dia => new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = dia,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0),
            Activo = true
        }).ToList();

        _disponibilidadRepoMock.Setup(r => r.GetByVeterinarioIdAsync(vetId)).ReturnsAsync(disponibilidades);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, It.IsAny<DateTime>(), null)).ReturnsAsync(false);

        List<Turno>? guardados = null;
        _turnoRepoMock.Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()))
            .Callback<IEnumerable<Turno>>(t => guardados = t.ToList())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.GenerarTurnosSeguimientoCronicoAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(6, result.TotalGenerados); // Mensual por 6 meses = 6 turnos
        Assert.Equal(0, result.TotalPendientes);
        Assert.All(result.TurnosGenerados, t =>
        {
            Assert.Equal("Pendiente", t.Estado);
            Assert.Equal("SeguimientoCronico", t.TipoOrigen);
            Assert.True(t.EsAutomatico);
            Assert.Equal(atencionId, t.AtencionMedicaId);
        });

        _turnoRepoMock.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()), Times.Once);
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronico_Tolerancia7Dias_EncuentraCupoEnDiaPosterior()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaAtencion = DateTime.UtcNow.Date;

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaAtencion = fechaAtencion,
            Frecuencia = "Trimestral", // 2 fechas: +3m y +6m
            JornadaPreferida = "Manana"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.ExisteSeguimientoParaAtencionAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        // Solo atiende los días miércoles
        var disponibilidades = new List<DisponibilidadVeterinario>
        {
            new DisponibilidadVeterinario
            {
                VeterinarioId = vetId,
                DiaSemana = DayOfWeek.Wednesday,
                Jornada = "Manana",
                HoraInicio = new TimeSpan(9, 0, 0),
                HoraFin = new TimeSpan(11, 0, 0),
                Activo = true
            }
        };

        _disponibilidadRepoMock.Setup(r => r.GetByVeterinarioIdAsync(vetId)).ReturnsAsync(disponibilidades);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, It.IsAny<DateTime>(), null)).ReturnsAsync(false);

        // Act
        var result = await _service.GenerarTurnosSeguimientoCronicoAsync(dto);

        // Assert: a pesar de que el día ideal puede no ser miércoles, dentro de la ventana de 7 días encuentra el miércoles
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalGenerados);
        Assert.All(result.TurnosGenerados, t => Assert.Equal(DayOfWeek.Wednesday, t.FechaHora.DayOfWeek));
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronico_SinCupoEnHorizonte_ReportaFechasPendientes()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaAtencion = DateTime.UtcNow.Date;

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaAtencion = fechaAtencion,
            Frecuencia = "Trimestral", // 2 fechas
            JornadaPreferida = "Tarde"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        // Profesional NO tiene jornada de tarde configurada
        var disponibilidades = new List<DisponibilidadVeterinario>
        {
            new DisponibilidadVeterinario
            {
                VeterinarioId = vetId,
                DiaSemana = DayOfWeek.Monday,
                Jornada = "Manana", // Solo mañana
                HoraInicio = new TimeSpan(8, 0, 0),
                HoraFin = new TimeSpan(12, 0, 0),
                Activo = true
            }
        };

        _disponibilidadRepoMock.Setup(r => r.GetByVeterinarioIdAsync(vetId)).ReturnsAsync(disponibilidades);

        // Act
        var result = await _service.GenerarTurnosSeguimientoCronicoAsync(dto);

        // Assert: respetando estrictamente la jornada seleccionada, no cambia de tarde a mañana, reportando las fechas como pendientes
        Assert.NotNull(result);
        Assert.Equal(0, result.TotalGenerados);
        Assert.Equal(2, result.TotalPendientes);
        Assert.Contains("no contaron con cupo disponible", result.Mensaje);
    }
}
