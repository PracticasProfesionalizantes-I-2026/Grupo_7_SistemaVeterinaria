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
    private readonly Mock<IAtencionMedicaRepository> _atencionRepoMock;
    private readonly TurnoService _service;

    public TurnoServiceTests()
    {
        _turnoRepoMock = new Mock<ITurnoRepository>();
        _disponibilidadRepoMock = new Mock<IDisponibilidadVeterinarioRepository>();
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _atencionRepoMock = new Mock<IAtencionMedicaRepository>();

        _service = new TurnoService(
            _turnoRepoMock.Object,
            _disponibilidadRepoMock.Object,
            _mascotaRepoMock.Object,
            _usuarioRepoMock.Object,
            _atencionRepoMock.Object);
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
    public async Task UpdateAsync_TurnoEnAtencion_LanzaTurnoEnAtencionException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoUpdateDTO { FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(10) };
        var turno = new Turno { Id = id, Estado = "EnAtencion" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoEnAtencionException>(() => _service.UpdateAsync(id, dto));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_SuperposicionConOtroTurno_LanzaHorarioNoDisponibleException()
    {
        var id = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);
        var dto = new TurnoUpdateDTO { FechaHora = nuevaFecha };

        var turno = new Turno
        {
            Id = id,
            MascotaId = mascotaId,
            Mascota = new Mascota { Id = mascotaId, Activa = true },
            VeterinarioId = vetId,
            Estado = "Confirmado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, nuevaFecha, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, nuevaFecha, id)).ReturnsAsync(true);

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => _service.UpdateAsync(id, dto));
    }

    [Fact]
    public async Task UpdateAsync_MascotaInactiva_LanzaMascotaInactivaExceptionYSinModificar()
    {
        // Arrange (BUG-05 / RN-07 / CU-06 flujo 3e: No reprogramar turnos de mascotas inactivas)
        var id = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var fechaOriginal = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);
        var dto = new TurnoUpdateDTO { FechaHora = nuevaFecha, MotivoConsulta = "Intento reprogramación" };

        var turno = new Turno
        {
            Id = id,
            MascotaId = mascotaId,
            Mascota = new Mascota { Id = mascotaId, Nombre = "Rocky", Activa = false },
            VeterinarioId = Guid.NewGuid(),
            FechaHora = fechaOriginal,
            MotivoConsulta = "Motivo original",
            Estado = "Confirmado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaInactivaException>(() => _service.UpdateAsync(id, dto));
        Assert.Equal(fechaOriginal, turno.FechaHora);
        Assert.Equal("Motivo original", turno.MotivoConsulta);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_MascotaAsociadaInexistente_LanzaMascotaNotFoundExceptionYSinModificar()
    {
        // Arrange (BUG-05: Si la mascota asociada no existe -> 404 MascotaNotFoundException sin modificar)
        var id = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var fechaOriginal = DateTime.UtcNow.AddDays(1).Date.AddHours(9);
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);
        var dto = new TurnoUpdateDTO { FechaHora = nuevaFecha, MotivoConsulta = "Intento reprogramación" };

        var turno = new Turno
        {
            Id = id,
            MascotaId = mascotaId,
            Mascota = null!,
            VeterinarioId = Guid.NewGuid(),
            FechaHora = fechaOriginal,
            MotivoConsulta = "Motivo original",
            Estado = "Confirmado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync((Mascota?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaNotFoundException>(() => _service.UpdateAsync(id, dto));
        Assert.Equal(fechaOriginal, turno.FechaHora);
        Assert.Equal("Motivo original", turno.MotivoConsulta);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Invalido")]
    [InlineData("EnCurso")]
    [InlineData("Completado")]
    public async Task GetAgendaAsync_EstadoInvalido_LanzaValidationException(string estadoInvalido)
    {
        // Arrange (BUG-04 / CU-05 flujo 1a)
        var filtro = new AgendaFilterDTO { Estado = estadoInvalido };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetAgendaAsync(filtro));
        _turnoRepoMock.Verify(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Never);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Confirmado")]
    [InlineData("EnAtencion")]
    [InlineData("Finalizado")]
    [InlineData("Cancelado")]
    [InlineData("confirmado")]
    [InlineData("enatencion")]
    public async Task GetAgendaAsync_EstadosValidos_PermiteConsultaYNormalizaEstado(string estadoValido)
    {
        // Arrange (BUG-04: Estados válidos permitidos)
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.GetAgendaAsync(null, null, vetId, It.IsAny<string?>()))
                      .ReturnsAsync(new List<Turno>());

        var filtro = new AgendaFilterDTO { VeterinarioId = vetId, Estado = estadoValido };

        // Act
        var result = await _service.GetAgendaAsync(filtro);

        // Assert
        Assert.NotNull(result);
        _turnoRepoMock.Verify(r => r.GetAgendaAsync(
            null,
            null,
            vetId,
            It.Is<string?>(s => string.Equals(s, estadoValido, StringComparison.OrdinalIgnoreCase))), Times.Once);
    }

    [Fact]
    public async Task GetAgendaAsync_VeterinarioInexistenteInactivoOSinRolVeterinario_LanzaVeterinarioNotFoundException()
    {
        // Arrange (BUG-04 / CU-05 flujo 3b)
        var vetInexistenteId = Guid.NewGuid();
        var vetInactivoId = Guid.NewGuid();
        var recepcionistaId = Guid.NewGuid();

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetInexistenteId)).ReturnsAsync((Usuario?)null);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetInactivoId)).ReturnsAsync(new Usuario { Id = vetInactivoId, Rol = "Veterinario", Activo = false });
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(recepcionistaId)).ReturnsAsync(new Usuario { Id = recepcionistaId, Rol = "Recepcionista", Activo = true });

        // Act & Assert
        await Assert.ThrowsAsync<VeterinarioNotFoundException>(() => _service.GetAgendaAsync(new AgendaFilterDTO { VeterinarioId = vetInexistenteId }));
        await Assert.ThrowsAsync<VeterinarioNotFoundException>(() => _service.GetAgendaAsync(new AgendaFilterDTO { VeterinarioId = vetInactivoId }));
        await Assert.ThrowsAsync<VeterinarioNotFoundException>(() => _service.GetAgendaAsync(new AgendaFilterDTO { VeterinarioId = recepcionistaId }));
        _turnoRepoMock.Verify(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GetAgendaAsync_FiltroConGuidEmpty_LanzaValidationException()
    {
        // Arrange (BUG-04: Guid.Empty proporcionado como filtro es inválido -> 400 ValidationException)
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetAgendaAsync(new AgendaFilterDTO { VeterinarioId = Guid.Empty }));
        await Assert.ThrowsAsync<ValidationException>(() => _service.GetAgendaAsync(new AgendaFilterDTO { MascotaId = Guid.Empty }));
        _turnoRepoMock.Verify(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Never);
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
    public async Task CancelarAsync_TurnoEnAtencion_LanzaTurnoEnAtencionException()
    {
        var id = Guid.NewGuid();
        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Intento de cancelación durante atención" };
        var turno = new Turno { Id = id, Estado = "EnAtencion" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(id)).ReturnsAsync(turno);

        await Assert.ThrowsAsync<TurnoEnAtencionException>(() =>
            _service.CancelarAsync(id, dto, Guid.NewGuid(), "recep.ana"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
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

    [Fact]
    public async Task ConfigurarDisponibilidad_DtoNuloOSinElementos_LanzaValidationException_SinGuardar()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, null!));

        var dtoVacio = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>()
        };

        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dtoVacio));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_ElementoNuloEnLista_LanzaDisponibilidadInvalidaException_SinGuardar()
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
                null!
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    [InlineData(99)]
    public async Task ConfigurarDisponibilidad_DiaSemanaInvalido_LanzaDisponibilidadInvalidaException_SinGuardar(int diaInvalido)
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
                    DiaSemana = (DayOfWeek)diaInvalido,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Noche")]
    [InlineData("Completa")]
    public async Task ConfigurarDisponibilidad_JornadaInvalidaOVacia_LanzaDisponibilidadInvalidaException_SinGuardar(string? jornadaInvalida)
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
                    Jornada = jornadaInvalida!,
                    HoraInicio = new TimeSpan(8, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfigurarDisponibilidad_JornadaConMayusculasYEspacios_NormalizaAMananaOTarde()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        List<DisponibilidadVeterinario>? guardadas = null;
        _disponibilidadRepoMock
            .Setup(r => r.SetDisponibilidadesAsync(vetId, It.IsAny<IEnumerable<DisponibilidadVeterinario>>()))
            .Callback<Guid, IEnumerable<DisponibilidadVeterinario>>((_, list) => guardadas = list.ToList())
            .Returns(Task.CompletedTask);

        var dto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "  mAnAnA  ",
                    HoraInicio = new TimeSpan(0, 0, 0),
                    HoraFin = new TimeSpan(12, 0, 0)
                },
                new DisponibilidadVeterinarioDTO
                {
                    DiaSemana = DayOfWeek.Tuesday,
                    Jornada = " TARDE ",
                    HoraInicio = new TimeSpan(14, 30, 0),
                    HoraFin = TimeSpan.FromHours(24)
                }
            }
        };

        var result = (await _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Manana", result[0].Jornada);
        Assert.Equal("Tarde", result[1].Jornada);
        Assert.NotNull(guardadas);
        Assert.Equal("Manana", guardadas![0].Jornada);
        Assert.Equal("Tarde", guardadas[1].Jornada);
    }

    [Theory]
    [InlineData(-30, 720)]   // HoraInicio negativa (-00:30)
    [InlineData(-120, 480)]  // HoraInicio negativa (-02:00)
    [InlineData(480, 1470)]  // HoraFin mayor a 24:00 (24:30)
    [InlineData(480, 1800)]  // HoraFin mayor a 24:00 (30:00)
    [InlineData(480, 480)]   // HoraInicio == HoraFin (08:00 == 08:00)
    public async Task ConfigurarDisponibilidad_HorarioFueraDeLas24HorasOInicioIgualFin_LanzaDisponibilidadInvalidaException_SinGuardar(
        int inicioMinutos,
        int finMinutos)
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
                    HoraInicio = TimeSpan.FromMinutes(inicioMinutos),
                    HoraFin = TimeSpan.FromMinutes(finMinutos)
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Theory]
    [InlineData(8, 15, 0, 0, 12, 0, 0, 0)]   // HoraInicio con minutos :15
    [InlineData(8, 0, 0, 0, 11, 45, 0, 0)]   // HoraFin con minutos :45
    [InlineData(8, 0, 10, 0, 12, 0, 0, 0)]   // HoraInicio con segundos
    [InlineData(8, 0, 0, 0, 12, 0, 1, 0)]    // HoraFin con segundos
    [InlineData(8, 0, 0, 250, 12, 0, 0, 0)]  // HoraInicio con milisegundos
    public async Task ConfigurarDisponibilidad_HorarioNoAlineadoABloqueDe30Minutos_LanzaDisponibilidadInvalidaException_SinGuardar(
        int hIni, int mIni, int sIni, int msIni,
        int hFin, int mFin, int sFin, int msFin)
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
                    DiaSemana = DayOfWeek.Wednesday,
                    Jornada = "Manana",
                    HoraInicio = new TimeSpan(0, hIni, mIni, sIni, msIni),
                    HoraFin = new TimeSpan(0, hFin, mFin, sFin, msFin)
                }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
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

    [Fact]
    public async Task ConfirmarAsync_TurnoExistenteEnEstadoPendiente_ConfirmaTurnoYRetornaTurnoResponseDTO()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9); // 09:00

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Pendiente",
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fechaFutura, turnoId)).ReturnsAsync(false);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        // Act
        var result = await _service.ConfirmarAsync(turnoId, usuarioId, "recep.ana");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Confirmado", turno.Estado);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Id == turnoId && t.Estado == "Confirmado")), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_TurnoInexistente_LanzaTurnoNotFoundException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync((Turno?)null);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNotFoundException>(() => _service.ConfirmarAsync(turnoId, Guid.NewGuid(), "recep.ana"));
    }

    [Theory]
    [InlineData("Confirmado")]
    [InlineData("EnAtencion")]
    [InlineData("Finalizado")]
    [InlineData("Cancelado")]
    public async Task ConfirmarAsync_TurnoNoEstaEnEstadoPendiente_LanzaTurnoNoPendienteException(string estadoNoPendiente)
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = Guid.NewGuid(),
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
            Estado = estadoNoPendiente
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNoPendienteException>(() => _service.ConfirmarAsync(turnoId, Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task ConfirmarAsync_MascotaInactiva_LanzaMascotaInactivaException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = false };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = Guid.NewGuid(),
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
            Estado = "Pendiente",
            Mascota = mascota
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaInactivaException>(() => _service.ConfirmarAsync(turnoId, Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task ConfirmarAsync_HorarioYaOcupadoPorTurnoConfirmado_LanzaHorarioNoDisponibleException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Pendiente",
            Mascota = mascota
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(true);
        // Simula superposición con un turno confirmado existente
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fechaFutura, turnoId)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() => _service.ConfirmarAsync(turnoId, Guid.NewGuid(), "recep.ana"));
        Assert.Equal("Pendiente", turno.Estado); // No cambió de estado
    }

    [Fact]
    public async Task CancelarAsync_ConMotivoInasistencia_CancelaTurnoYRegistraMotivo()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = Guid.NewGuid(),
            FechaHora = DateTime.UtcNow.AddDays(1).Date.AddHours(10),
            Estado = "Confirmado"
        };

        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Inasistencia" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        // Act
        var result = await _service.CancelarAsync(turnoId, dto, usuarioId, "recep.ana");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cancelado", turno.Estado);
        Assert.Equal("Inasistencia", turno.MotivoCancelacion);
        Assert.Equal(usuarioId, turno.UsuarioCancelacionId);
        Assert.Equal("recep.ana", turno.UsuarioCancelacionUsername);
        Assert.NotNull(turno.FechaCancelacion);
    }

    [Fact]
    public async Task ConfirmarAsync_DosConfirmacionesSimultaneasTurnosSuperpuestos_UnaSolaTieneExitoYLaOtraLanzaHorarioNoDisponibleException()
    {
        // Arrange
        var vetId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9); // 09:00

        var mascota1 = new Mascota { Id = Guid.NewGuid(), Nombre = "Milo", Activa = true };
        var mascota2 = new Mascota { Id = Guid.NewGuid(), Nombre = "Luna", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        var turno1Id = Guid.NewGuid();
        var turno2Id = Guid.NewGuid();

        // Turno 1 a las 09:00, Turno 2 a las 09:15 (superposición parcial en el bloque de 30m)
        var turno1 = new Turno
        {
            Id = turno1Id,
            MascotaId = mascota1.Id,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Pendiente",
            Mascota = mascota1,
            Veterinario = vet
        };

        var turno2 = new Turno
        {
            Id = turno2Id,
            MascotaId = mascota2.Id,
            VeterinarioId = vetId,
            FechaHora = fechaFutura.AddMinutes(15),
            Estado = "Pendiente",
            Mascota = mascota2,
            Veterinario = vet
        };

        var turnosConfirmados = new List<Turno>();
        var lockObj = new object();

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turno1Id)).ReturnsAsync(turno1);
        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turno2Id)).ReturnsAsync(turno2);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => id == turno1Id ? turno1 : turno2);

        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, It.IsAny<DateTime>(), 30)).ReturnsAsync(true);

        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, It.IsAny<DateTime>(), It.IsAny<Guid?>()))
            .ReturnsAsync((Guid vId, DateTime fh, Guid? idExcluir) =>
            {
                lock (lockObj)
                {
                    var inicio = fh.AddMinutes(-30);
                    var fin = fh.AddMinutes(30);
                    return turnosConfirmados.Any(t => t.VeterinarioId == vId && t.Id != idExcluir && t.FechaHora > inicio && t.FechaHora < fin);
                }
            });

        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>()))
            .ReturnsAsync((Turno t) =>
            {
                lock (lockObj)
                {
                    if (t.Estado == "Confirmado" && !turnosConfirmados.Any(x => x.Id == t.Id))
                    {
                        turnosConfirmados.Add(t);
                    }
                }
                return t;
            });

        // Act: Dos confirmaciones simultáneas para el mismo veterinario en horarios superpuestos
        var task1 = Task.Run(() => _service.ConfirmarAsync(turno1Id, usuarioId, "recep.ana"));
        var task2 = Task.Run(() => _service.ConfirmarAsync(turno2Id, usuarioId, "recep.ana"));

        var resultados = await Task.WhenAll(
            task1.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException)),
            task2.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException))
        );

        // Assert: Exactamente una petición debe tener éxito y la otra debe ser rechazada por colisión horaria
        var exitosos = resultados.Count(r => r.Exitoso);
        var conflictos = resultados.Count(r => r.Exception is HorarioNoDisponibleException);

        Assert.Equal(1, exitosos);
        Assert.Equal(1, conflictos);
        Assert.Single(turnosConfirmados);
    }

    [Fact]
    public async Task ConfirmarAsync_DosConfirmacionesSimultaneasMismoTurno_UnaSolaTieneExitoYLaOtraLanzaTurnoNoPendienteException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(10);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Pendiente",
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fechaFutura, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fechaFutura, turnoId)).ReturnsAsync(false);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync((Turno t) =>
        {
            turno.Estado = t.Estado;
            return t;
        });
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        // Act: Dos peticiones simultáneas intentando confirmar el mismo turno
        var task1 = Task.Run(() => _service.ConfirmarAsync(turnoId, usuarioId, "recep.ana"));
        var task2 = Task.Run(() => _service.ConfirmarAsync(turnoId, usuarioId, "recep.ana"));

        var resultados = await Task.WhenAll(
            task1.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException)),
            task2.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException))
        );

        // Assert: Exactamente una confirmación tiene éxito y la segunda detecta que ya no está pendiente
        var exitosos = resultados.Count(r => r.Exitoso);
        var conflictos = resultados.Count(r => r.Exception is TurnoNoPendienteException);

        Assert.Equal(1, exitosos);
        Assert.Equal(1, conflictos);
        Assert.Equal("Confirmado", turno.Estado);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Id == turnoId && t.Estado == "Confirmado")), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_EntidadPreviamenteRastreadaConEstadoDesactualizado_RecargaEstadoDesdeBDYLanzaTurnoNoPendienteException()
    {
        // Arrange: El DbContext ya tenía el turno rastreado en memoria como "Pendiente"
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Pendiente",
            Mascota = mascota
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Al ejecutar ReloadAsync dentro del semáforo, EF Core recarga desde la BD descubriendo que ya fue "Confirmado"
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno))
            .Callback<Turno>(t => t.Estado = "Confirmado")
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNoPendienteException>(() => _service.ConfirmarAsync(turnoId, Guid.NewGuid(), "recep.ana"));
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_EntidadPreviamenteRastreadaConEstadoDesactualizado_RecargaEstadoDesdeBDYLanzaTurnoFinalizadoException()
    {
        // Arrange: El DbContext tenía rastreado el turno como "Confirmado"
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(11);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Confirmado",
            Mascota = mascota
        };

        var dto = new TurnoUpdateDTO
        {
            FechaHora = fechaFutura.AddHours(1),
            MotivoConsulta = "Consulta reprogramada"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Al ejecutar ReloadAsync dentro del semáforo, se descubre que el turno ya fue "Finalizado" en la BD
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno))
            .Callback<Turno>(t => t.Estado = "Finalizado")
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoFinalizadoException>(() => _service.UpdateAsync(turnoId, dto));
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task CancelarAsync_EntidadPreviamenteRastreadaConEstadoDesactualizado_RecargaEstadoDesdeBDYLanzaTurnoYaCanceladoException()
    {
        // Arrange: El DbContext tenía rastreado el turno como "Confirmado"
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(11);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fechaFutura,
            Estado = "Confirmado",
            Mascota = mascota
        };

        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Cancelación por cliente" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Al ejecutar ReloadAsync dentro del semáforo, se descubre que ya fue "Cancelado" concurrentemente
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno))
            .Callback<Turno>(t => t.Estado = "Cancelado")
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoYaCanceladoException>(() => _service.CancelarAsync(turnoId, dto, Guid.NewGuid(), "recep.ana"));
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    // --- INICIO DE ATENCIÓN Y FINALIZACIÓN MANUAL (CU-06 / RN-09) ---

    [Fact]
    public async Task IniciarAtencionAsync_TurnoConfirmadoYVeterinarioAsignado_CambiaEstadoAEnAtencionYRegistraFechaHoraInicioUtc()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fecha = DateTime.UtcNow.AddHours(1);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fecha,
            Estado = "Confirmado",
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        var antesUtc = DateTime.UtcNow;

        // Act
        var result = await _service.IniciarAtencionAsync(turnoId, vetId, "vet.carlos");

        var despuesUtc = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("EnAtencion", turno.Estado);
        Assert.Equal("EnAtencion", result.Estado);
        Assert.NotNull(turno.FechaHoraInicioAtencion);
        Assert.NotNull(result.FechaHoraInicioAtencion);
        Assert.InRange(turno.FechaHoraInicioAtencion!.Value, antesUtc, despuesUtc);
        Assert.Null(turno.FechaHoraFinAtencion);
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Id == turnoId && t.Estado == "EnAtencion" && t.FechaHoraInicioAtencion.HasValue)), Times.Once);
    }

    [Fact]
    public async Task IniciarAtencionAsync_TurnoInexistente_LanzaTurnoNotFoundException()
    {
        var turnoId = Guid.NewGuid();
        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync((Turno?)null);

        await Assert.ThrowsAsync<TurnoNotFoundException>(() => _service.IniciarAtencionAsync(turnoId, Guid.NewGuid(), "vet.carlos"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("EnAtencion")]
    [InlineData("Cancelado")]
    [InlineData("Finalizado")]
    public async Task IniciarAtencionAsync_TurnoNoEstaEnEstadoConfirmado_LanzaTurnoNoConfirmadoException(string estadoInvalido)
    {
        var turnoId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = estadoInvalido
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoNoConfirmadoException>(() => _service.IniciarAtencionAsync(turnoId, vetId, "vet.carlos"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task IniciarAtencionAsync_OtroVeterinarioIntentaIniciar_LanzaTurnoVeterinarioNoAutorizadoException()
    {
        var turnoId = Guid.NewGuid();
        var vetAsignadoId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();

        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetAsignadoId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = "Confirmado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() => _service.IniciarAtencionAsync(turnoId, otroVetId, "vet.otro"));
        Assert.Equal("Confirmado", turno.Estado);
        Assert.Null(turno.FechaHoraInicioAtencion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Recepcionista")]
    [InlineData("Administrador")]
    public async Task IniciarAtencionAsync_RolNoAutorizado_LanzaTurnoVeterinarioNoAutorizadoException(string rolNoAutorizado)
    {
        var turnoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var usuarioNoVet = new Usuario { Id = usuarioId, Username = "usuario.no.vet", Rol = rolNoAutorizado, Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = usuarioId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = "Confirmado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(usuarioId)).ReturnsAsync(usuarioNoVet);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() => _service.IniciarAtencionAsync(turnoId, usuarioId, "usuario.no.vet"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task IniciarAtencionAsync_DosIniciosSimultaneosMismoTurno_UnoSoloTieneExitoYElOtroLanzaTurnoNoConfirmadoException()
    {
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fecha = DateTime.UtcNow.AddHours(2);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fecha,
            Estado = "Confirmado",
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync((Turno t) =>
        {
            turno.Estado = t.Estado;
            turno.FechaHoraInicioAtencion = t.FechaHoraInicioAtencion;
            return t;
        });
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        var task1 = Task.Run(() => _service.IniciarAtencionAsync(turnoId, vetId, "vet.carlos"));
        var task2 = Task.Run(() => _service.IniciarAtencionAsync(turnoId, vetId, "vet.carlos"));

        var resultados = await Task.WhenAll(
            task1.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException)),
            task2.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException))
        );

        var exitosos = resultados.Count(r => r.Exitoso);
        var conflictos = resultados.Count(r => r.Exception is TurnoNoConfirmadoException);

        Assert.Equal(1, exitosos);
        Assert.Equal(1, conflictos);
        Assert.Equal("EnAtencion", turno.Estado);
        Assert.NotNull(turno.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task FinalizarAsync_TurnoEnAtencionConAtencionMedicaYVeterinarioAsignado_CambiaEstadoAFinalizadoYRegistraFechaHoraFinUtc()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fecha = DateTime.UtcNow.AddHours(2);
        var fechaInicio = DateTime.UtcNow.AddMinutes(-15);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fecha,
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = fechaInicio,
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _atencionRepoMock.Setup(r => r.ExistsByTurnoIdAsync(turnoId)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        // Act
        var result = await _service.FinalizarAsync(turnoId, vetId, "vet.carlos");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Finalizado", turno.Estado);
        Assert.Equal("Finalizado", result.Estado);
        Assert.NotNull(turno.FechaHoraFinAtencion);
        Assert.NotNull(result.FechaHoraFinAtencion);
        Assert.True(turno.FechaHoraFinAtencion >= turno.FechaHoraInicioAtencion);
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _atencionRepoMock.Verify(r => r.ExistsByTurnoIdAsync(turnoId), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Id == turnoId && t.Estado == "Finalizado" && t.FechaHoraFinAtencion.HasValue)), Times.Once);
    }

    [Fact]
    public async Task FinalizarAsync_TurnoInexistente_LanzaTurnoNotFoundException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync((Turno?)null);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNotFoundException>(() => _service.FinalizarAsync(turnoId, Guid.NewGuid(), "vet.carlos"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Pendiente")]
    [InlineData("Confirmado")]
    [InlineData("Cancelado")]
    [InlineData("Finalizado")]
    public async Task FinalizarAsync_TurnoNoEstaEnEstadoEnAtencion_LanzaTurnoNoEnAtencionException(string estadoInvalido)
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddHours(2),
            Estado = estadoInvalido
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNoEnAtencionException>(() => _service.FinalizarAsync(turnoId, vetId, "vet.carlos"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_TurnoEnAtencionSinAtencionMedicaVinculada_LanzaTurnoSinAtencionMedicaException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow.AddMinutes(-10),
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _atencionRepoMock.Setup(r => r.ExistsByTurnoIdAsync(turnoId)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoSinAtencionMedicaException>(() => _service.FinalizarAsync(turnoId, vetId, "vet.carlos"));
        Assert.Equal("EnAtencion", turno.Estado);
        Assert.Null(turno.FechaHoraFinAtencion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_OtroVeterinarioIntentaFinalizar_LanzaTurnoVeterinarioNoAutorizadoException()
    {
        var turnoId = Guid.NewGuid();
        var vetAsignadoId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();

        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetAsignadoId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow.AddMinutes(-10)
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() => _service.FinalizarAsync(turnoId, otroVetId, "vet.otro"));
        Assert.Equal("EnAtencion", turno.Estado);
        Assert.Null(turno.FechaHoraFinAtencion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Recepcionista")]
    [InlineData("Administrador")]
    public async Task FinalizarAsync_RolNoAutorizado_LanzaTurnoVeterinarioNoAutorizadoException(string rolNoAutorizado)
    {
        var turnoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();
        var usuarioNoVet = new Usuario { Id = usuarioId, Username = "usuario.no.vet", Rol = rolNoAutorizado, Activo = true };

        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = usuarioId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow.AddMinutes(-10)
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(usuarioId)).ReturnsAsync(usuarioNoVet);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() => _service.FinalizarAsync(turnoId, usuarioId, "usuario.no.vet"));
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_EntidadPreviamenteRastreadaConEstadoDesactualizado_RecargaEstadoDesdeBDYLanzaTurnoNoEnAtencionException()
    {
        // Arrange: DbContext tenía el turno rastreado como "EnAtencion"
        var turnoId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetId,
            FechaHora = DateTime.UtcNow.AddHours(2),
            Estado = "EnAtencion"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);

        // Al ejecutar ReloadAsync dentro del semáforo, se descubre que ya fue finalizado en la BD
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno))
            .Callback<Turno>(t => t.Estado = "Finalizado")
            .Returns(Task.CompletedTask);

        // Act & Assert
        await Assert.ThrowsAsync<TurnoNoEnAtencionException>(() => _service.FinalizarAsync(turnoId, vetId, "vet.carlos"));
        _turnoRepoMock.Verify(r => r.ReloadAsync(turno), Times.Once);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task FinalizarAsync_DosFinalizacionesSimultaneasMismoTurno_UnaSolaTieneExitoYLaOtraLanzaTurnoNoEnAtencionException()
    {
        // Arrange
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fecha = DateTime.UtcNow.AddHours(3);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fecha,
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow.AddMinutes(-10),
            Mascota = mascota,
            Veterinario = vet
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);
        _atencionRepoMock.Setup(r => r.ExistsByTurnoIdAsync(turnoId)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Turno>())).ReturnsAsync((Turno t) =>
        {
            turno.Estado = t.Estado;
            turno.FechaHoraFinAtencion = t.FechaHoraFinAtencion;
            return t;
        });
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        // Act: Dos peticiones simultáneas intentando finalizar el mismo turno en atención
        var task1 = Task.Run(() => _service.FinalizarAsync(turnoId, vetId, "vet.carlos"));
        var task2 = Task.Run(() => _service.FinalizarAsync(turnoId, vetId, "vet.carlos"));

        var resultados = await Task.WhenAll(
            task1.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException)),
            task2.ContinueWith(t => (Exitoso: t.IsCompletedSuccessfully, Exception: t.Exception?.InnerException))
        );

        // Assert: Exactamente una finalización tiene éxito y la segunda es rechazada por estado ya no EnAtencion
        var exitosos = resultados.Count(r => r.Exitoso);
        var conflictos = resultados.Count(r => r.Exception is TurnoNoEnAtencionException);

        Assert.Equal(1, exitosos);
        Assert.Equal(1, conflictos);
        Assert.Equal("Finalizado", turno.Estado);
        Assert.NotNull(turno.FechaHoraFinAtencion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.Is<Turno>(t => t.Id == turnoId && t.Estado == "Finalizado")), Times.Once);
    }

    // --- AUTORIZACIÓN HORIZONTAL EN GESTIÓN DE TURNOS (Create, Confirmar, Update, Cancelar) ---

    [Fact]
    public async Task CreateManualAsync_VeterinarioIntentaCrearTurnoParaOtroVeterinario_LanzaTurnoVeterinarioNoAutorizadoException_SinCrear()
    {
        var vetAutenticadoId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();
        var dto = new TurnoCreateDTO
        {
            MascotaId = Guid.NewGuid(),
            VeterinarioId = otroVetId,
            FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
            MotivoConsulta = "Intento de crear turno para otro colega"
        };

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.CreateManualAsync(dto, vetAutenticadoId, "Veterinario"));

        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Theory]
    [InlineData("Veterinario", true)]
    [InlineData("Recepcionista", false)]
    public async Task CreateManualAsync_VeterinarioPropioORecepcionistaCualquierVet_PermiteCreacion(string rol, bool mismoId)
    {
        var mascotaId = Guid.NewGuid();
        var vetObjetivoId = Guid.NewGuid();
        var usuarioAutenticadoId = mismoId ? vetObjetivoId : Guid.NewGuid();
        var fechaFutura = DateTime.UtcNow.AddDays(2).Date.AddHours(9);

        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetObjetivoId,
            FechaHora = fechaFutura,
            MotivoConsulta = "Control general"
        };

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var vet = new Usuario { Id = vetObjetivoId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetObjetivoId)).ReturnsAsync(vet);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetObjetivoId, fechaFutura, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetObjetivoId, fechaFutura, null)).ReturnsAsync(false);
        _turnoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Turno>())).ReturnsAsync((Turno t) => t);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => new Turno
        {
            Id = id,
            MascotaId = mascotaId,
            VeterinarioId = vetObjetivoId,
            FechaHora = fechaFutura,
            MotivoConsulta = dto.MotivoConsulta,
            Estado = "Confirmado",
            Mascota = mascota,
            Veterinario = vet
        });

        var result = await _service.CreateManualAsync(dto, usuarioAutenticadoId, rol);

        Assert.NotNull(result);
        Assert.Equal(vetObjetivoId, result.VeterinarioId);
        _turnoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Turno>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmarAsync_VeterinarioIntentaConfirmarTurnoDeOtroVeterinario_LanzaTurnoVeterinarioNoAutorizadoException_SinModificar()
    {
        var turnoId = Guid.NewGuid();
        var vetTitularId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetTitularId,
            FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
            Estado = "Pendiente"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.ConfirmarAsync(turnoId, otroVetId, "vet.otro", "Veterinario"));

        Assert.Equal("Pendiente", turno.Estado);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_VeterinarioIntentaModificarTurnoDeOtroVeterinario_LanzaTurnoVeterinarioNoAutorizadoException_SinModificar()
    {
        var turnoId = Guid.NewGuid();
        var vetTitularId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();
        var fechaOriginal = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetTitularId,
            FechaHora = fechaOriginal,
            MotivoConsulta = "Motivo original",
            Estado = "Confirmado"
        };

        var dto = new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddHours(1),
            MotivoConsulta = "Motivo alterado"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.UpdateAsync(turnoId, dto, otroVetId, "Veterinario"));

        Assert.Equal(fechaOriginal, turno.FechaHora);
        Assert.Equal("Motivo original", turno.MotivoConsulta);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_VeterinarioIntentaReasignarSuTurnoAOtroVeterinario_LanzaTurnoVeterinarioNoAutorizadoException_SinModificar()
    {
        var turnoId = Guid.NewGuid();
        var vetTitularId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();
        var fechaOriginal = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetTitularId,
            FechaHora = fechaOriginal,
            MotivoConsulta = "Motivo original",
            Estado = "Confirmado"
        };

        var dto = new TurnoUpdateDTO
        {
            FechaHora = fechaOriginal.AddHours(1),
            VeterinarioId = otroVetId,
            MotivoConsulta = "Intento de reasignar veterinario"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.UpdateAsync(turnoId, dto, vetTitularId, "Veterinario"));

        Assert.Equal(vetTitularId, turno.VeterinarioId);
        Assert.Equal(fechaOriginal, turno.FechaHora);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_RecepcionistaReasignaTurnoAOtroVeterinario_PermiteModificacionExitosa()
    {
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetOriginalId = Guid.NewGuid();
        var nuevoVetId = Guid.NewGuid();
        var recepcionistaId = Guid.NewGuid();
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var nuevoVet = new Usuario { Id = nuevoVetId, Username = "vet.laura", Rol = "Veterinario", Activo = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            Mascota = mascota,
            VeterinarioId = vetOriginalId,
            FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
            MotivoConsulta = "Motivo original",
            Estado = "Confirmado"
        };

        var dto = new TurnoUpdateDTO
        {
            FechaHora = nuevaFecha,
            VeterinarioId = nuevoVetId,
            MotivoConsulta = "Reasignado por recepción"
        };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(nuevoVetId)).ReturnsAsync(nuevoVet);
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(nuevoVetId, nuevaFecha, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(nuevoVetId, nuevaFecha, turnoId)).ReturnsAsync(false);
        _turnoRepoMock.Setup(r => r.UpdateAsync(turno)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.GetByIdAsync(turnoId)).ReturnsAsync(turno);

        var result = await _service.UpdateAsync(turnoId, dto, recepcionistaId, "Recepcionista");

        Assert.NotNull(result);
        Assert.Equal(nuevoVetId, turno.VeterinarioId);
        Assert.Equal(nuevaFecha, turno.FechaHora);
        Assert.Equal("Reasignado por recepción", turno.MotivoConsulta);
        _turnoRepoMock.Verify(r => r.UpdateAsync(turno), Times.Once);
    }

    [Fact]
    public async Task CancelarAsync_VeterinarioIntentaCancelarTurnoDeOtroVeterinario_LanzaTurnoVeterinarioNoAutorizadoException_SinModificar()
    {
        var turnoId = Guid.NewGuid();
        var vetTitularId = Guid.NewGuid();
        var otroVetId = Guid.NewGuid();
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = Guid.NewGuid(),
            VeterinarioId = vetTitularId,
            FechaHora = DateTime.UtcNow.AddDays(2).Date.AddHours(9),
            Estado = "Confirmado"
        };

        var dto = new TurnoCancelarDTO { MotivoCancelacion = "Intento de cancelación ajena" };

        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock.Setup(r => r.ReloadAsync(turno)).Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.CancelarAsync(turnoId, dto, otroVetId, "vet.otro", "Veterinario"));

        Assert.Equal("Confirmado", turno.Estado);
        Assert.Null(turno.MotivoCancelacion);
        Assert.Null(turno.FechaCancelacion);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    [Fact]
    public async Task ConfigurarDisponibilidadVeterinarioAsync_Concurrencia_SincronizaReemplazoPorVeterinario()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        var ejecucionesEnCurso = 0;
        var maximoConcurrenteDetectado = 0;

        _disponibilidadRepoMock
            .Setup(r => r.SetDisponibilidadesAsync(vetId, It.IsAny<IEnumerable<DisponibilidadVeterinario>>()))
            .Returns(async () =>
            {
                var actual = Interlocked.Increment(ref ejecucionesEnCurso);
                int maxPrevio;
                do
                {
                    maxPrevio = maximoConcurrenteDetectado;
                    if (actual <= maxPrevio) break;
                } while (Interlocked.CompareExchange(ref maximoConcurrenteDetectado, actual, maxPrevio) != maxPrevio);

                await Task.Delay(40);
                Interlocked.Decrement(ref ejecucionesEnCurso);
            });

        var dto1 = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(12) }
            }
        };

        var dto2 = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Tuesday, Jornada = "Tarde", HoraInicio = TimeSpan.FromHours(14), HoraFin = TimeSpan.FromHours(18) }
            }
        };

        await Task.WhenAll(
            Task.Run(() => _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto1)),
            Task.Run(() => _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto2))
        );

        Assert.Equal(1, maximoConcurrenteDetectado);
        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(vetId, It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronicoAsync_ConcurrenciaMismaAtencionMedica_SoloPermiteUnaSerieYLanzaExcepcionEnLaSegunda()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var atencionId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, DuenoId = Guid.NewGuid() };
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        var seguimientoPersistido = false;
        _turnoRepoMock
            .Setup(r => r.ExisteSeguimientoParaAtencionAsync(atencionId))
            .ReturnsAsync(() => seguimientoPersistido);

        _disponibilidadRepoMock
            .Setup(r => r.GetByVeterinarioIdAsync(vetId))
            .ReturnsAsync(new List<DisponibilidadVeterinario>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    VeterinarioId = vetId,
                    DiaSemana = DayOfWeek.Monday,
                    Jornada = "Manana",
                    HoraInicio = TimeSpan.FromHours(9),
                    HoraFin = TimeSpan.FromHours(12),
                    Activo = true
                }
            });

        _turnoRepoMock
            .Setup(r => r.ExisteSuperposicionAsync(vetId, It.IsAny<DateTime>(), null))
            .ReturnsAsync(false);

        _turnoRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()))
            .Returns(async () =>
            {
                await Task.Delay(30);
                seguimientoPersistido = true;
            });

        var dto = new GenerarTurnosCronicosDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            AtencionMedicaId = atencionId,
            FechaAtencion = DateTime.UtcNow,
            Frecuencia = "Mensual",
            JornadaPreferida = "Manana"
        };

        var t1 = Task.Run(() => _service.GenerarTurnosSeguimientoCronicoAsync(dto));
        var t2 = Task.Run(() => _service.GenerarTurnosSeguimientoCronicoAsync(dto));

        var resultados = await Task.WhenAll(
            t1.ContinueWith(t => (Exito: t.IsCompletedSuccessfully, Ex: t.Exception?.InnerException)),
            t2.ContinueWith(t => (Exito: t.IsCompletedSuccessfully, Ex: t.Exception?.InnerException))
        );

        Assert.Equal(1, resultados.Count(r => r.Exito));
        Assert.Equal(1, resultados.Count(r => r.Ex is SeguimientoCronicoDuplicadoException));
        _turnoRepoMock.Verify(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_CuandoOtroHiloCambiaVeterinarioIdAntesDeAdquirirBloqueo_ReintentaYValidaConElVeterinarioActual()
    {
        var turnoId = Guid.NewGuid();
        var mascotaId = Guid.NewGuid();
        var vetInicialId = Guid.NewGuid();
        var vetReasignadoConcurrenteId = Guid.NewGuid();
        var fechaOriginal = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var nuevaFecha = DateTime.UtcNow.AddDays(3).Date.AddHours(10);

        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        var turno = new Turno
        {
            Id = turnoId,
            MascotaId = mascotaId,
            Mascota = mascota,
            VeterinarioId = vetInicialId,
            FechaHora = fechaOriginal,
            MotivoConsulta = "Control inicial",
            Estado = "Confirmado"
        };

        var dto = new TurnoUpdateDTO
        {
            FechaHora = nuevaFecha,
            VeterinarioId = vetInicialId,
            MotivoConsulta = "Intento de vetInicial tras reasignación concurrente"
        };

        var llamadasReload = 0;
        _turnoRepoMock.Setup(r => r.GetByIdTrackedAsync(turnoId)).ReturnsAsync(turno);
        _turnoRepoMock
            .Setup(r => r.ReloadAsync(turno))
            .Callback(() =>
            {
                llamadasReload++;
                if (llamadasReload == 1)
                {
                    // Simula que otra operación concurrente reasignó el turno a otro veterinario antes del primer lock
                    turno.VeterinarioId = vetReasignadoConcurrenteId;
                }
            })
            .Returns(Task.CompletedTask);

        // Act & Assert: vetInicialId ya no es dueño del turno tras el ReloadAsync y el reintento bajo el nuevo lock lo rechaza con 403
        await Assert.ThrowsAsync<TurnoVeterinarioNoAutorizadoException>(() =>
            _service.UpdateAsync(turnoId, dto, vetInicialId, "Veterinario"));

        Assert.Equal(2, llamadasReload);
        _turnoRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Turno>()), Times.Never);
    }

    // --- ETAPA C: VALIDACIÓN DE DUPLICADOS Y TRADUCCIÓN DE RESTRICCIONES ÚNICAS A 409 ---

    [Fact]
    public async Task ConfigurarDisponibilidad_MismoDiaYJornadaDuplicadosSinSuperposicionHoraria_LanzaDisponibilidadInvalidaException()
    {
        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Username = "vet.carlos", Rol = "Veterinario", Activo = true };
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(vet);

        var dto = new ConfigurarDisponibilidadDTO
        {
            VeterinarioId = vetId,
            Disponibilidades = new List<DisponibilidadVeterinarioDTO>
            {
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = TimeSpan.FromHours(8), HoraFin = TimeSpan.FromHours(10) },
                new() { DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = TimeSpan.FromHours(10), HoraFin = TimeSpan.FromHours(12) }
            }
        };

        await Assert.ThrowsAsync<DisponibilidadInvalidaException>(() =>
            _service.ConfigurarDisponibilidadVeterinarioAsync(vetId, dto));

        _disponibilidadRepoMock.Verify(
            r => r.SetDisponibilidadesAsync(It.IsAny<Guid>(), It.IsAny<IEnumerable<DisponibilidadVeterinario>>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateManualAsync_CuandoOcurreViolacionUnicidadEnBaseDeDatos_TraduceAHorarioNoDisponibleException()
    {
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var fecha = DateTime.UtcNow.AddDays(2).Date.AddHours(9);
        var dto = new TurnoCreateDTO
        {
            MascotaId = mascotaId,
            VeterinarioId = vetId,
            FechaHora = fecha,
            MotivoConsulta = "Control"
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(new Mascota { Id = mascotaId, Activa = true });
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(new Usuario { Id = vetId, Rol = "Veterinario", Activo = true });
        _disponibilidadRepoMock.Setup(r => r.EstaDentroDeDisponibilidadAsync(vetId, fecha, 30)).ReturnsAsync(true);
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, fecha, null)).ReturnsAsync(false);
        _turnoRepoMock
            .Setup(r => r.CreateAsync(It.IsAny<Turno>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException(
                "Error al guardar",
                new Exception("SQLite Error 19: 'UNIQUE constraint failed: Turnos.VeterinarioId, Turnos.FechaHora'.")));

        await Assert.ThrowsAsync<HorarioNoDisponibleException>(() =>
            _service.CreateManualAsync(dto, vetId, "Veterinario"));
    }

    [Fact]
    public async Task GenerarTurnosSeguimientoCronicoAsync_CuandoOcurreViolacionUnicidadEnBaseDeDatos_TraduceASeguimientoCronicoDuplicadoException_YNoTraduceOtrosErrores()
    {
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

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(new Mascota { Id = mascotaId, Activa = true });
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(vetId)).ReturnsAsync(new Usuario { Id = vetId, Rol = "Veterinario", Activo = true });
        _turnoRepoMock.Setup(r => r.ExisteSeguimientoParaAtencionAsync(atencionId)).ReturnsAsync(false);
        _disponibilidadRepoMock.Setup(r => r.GetByVeterinarioIdAsync(vetId)).ReturnsAsync(new List<DisponibilidadVeterinario>
        {
            new() { VeterinarioId = vetId, DiaSemana = DayOfWeek.Monday, Jornada = "Manana", HoraInicio = TimeSpan.FromHours(9), HoraFin = TimeSpan.FromHours(12), Activo = true }
        });
        _turnoRepoMock.Setup(r => r.ExisteSuperposicionAsync(vetId, It.IsAny<DateTime>(), null)).ReturnsAsync(false);

        // 1. Violación de unicidad en AtencionMedicaId, FechaHora se traduce a SeguimientoCronicoDuplicadoException (409)
        _turnoRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException(
                "Error",
                new Exception("SQLite Error 19: 'UNIQUE constraint failed: Turnos.AtencionMedicaId, Turnos.FechaHora'.")));

        await Assert.ThrowsAsync<SeguimientoCronicoDuplicadoException>(() =>
            _service.GenerarTurnosSeguimientoCronicoAsync(dto));

        // 2. Una DbUpdateException genérica no relacionada con unicidad NO debe traducirse a 409
        _turnoRepoMock
            .Setup(r => r.CreateRangeAsync(It.IsAny<IEnumerable<Turno>>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Falla técnica de conexión", new Exception("Disk I/O error")));

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(() =>
            _service.GenerarTurnosSeguimientoCronicoAsync(dto));
    }
}
