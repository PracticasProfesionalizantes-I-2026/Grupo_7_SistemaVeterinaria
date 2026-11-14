using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Context;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Turno;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class TurnoYAtencionMedicaRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly VeterinariaDbContext _context;
    private readonly TurnoRepository _turnoRepository;
    private readonly AtencionMedicaRepository _atencionRepository;

    private readonly Guid _vetId = Guid.NewGuid();
    private readonly Guid _duenoId = Guid.NewGuid();
    private readonly Guid _mascotaId = Guid.NewGuid();

    public TurnoYAtencionMedicaRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<VeterinariaDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new VeterinariaDbContext(options);
        _context.Database.EnsureCreated();

        _turnoRepository = new TurnoRepository(_context);
        _atencionRepository = new AtencionMedicaRepository(_context);

        SembrarDatosBase();
    }

    private void SembrarDatosBase()
    {
        var vet = new Usuario
        {
            Id = _vetId,
            Username = "vet.repo",
            Email = "vet.repo@patitas.com",
            PasswordHash = "hash",
            Rol = "Veterinario",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        var dueno = new Dueno
        {
            Id = _duenoId,
            Nombre = "Carlos",
            Apellido = "Gomez",
            Dni = "30111222",
            Telefono = "1122334455",
            Email = "carlos@test.com",
            FechaRegistro = DateTime.UtcNow
        };

        var mascota = new Mascota
        {
            Id = _mascotaId,
            Nombre = "Firulais",
            Especie = "Perro",
            Raza = "Mestizo",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-3),
            Peso = 15.5m,
            Activa = true,
            DuenoId = _duenoId
        };

        _context.Usuarios.Add(vet);
        _context.Duenos.Add(dueno);
        _context.Mascotas.Add(mascota);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ExisteSuperposicionAsync_ConTurnoEnAtencion_DevuelveTrueYBloqueaDisponibilidad()
    {
        // Arrange
        var fechaHoraBase = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        var turnoEnAtencion = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = fechaHoraBase,
            MotivoConsulta = "Consulta en curso",
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow
        };
        await _turnoRepository.CreateAsync(turnoEnAtencion);

        // Act
        var superponeMismaHora = await _turnoRepository.ExisteSuperposicionAsync(_vetId, fechaHoraBase);
        var superponeDentroDe30Min = await _turnoRepository.ExisteSuperposicionAsync(_vetId, fechaHoraBase.AddMinutes(15));
        var superponeJustoEnBorde30Min = await _turnoRepository.ExisteSuperposicionAsync(_vetId, fechaHoraBase.AddMinutes(30));
        var superponeExcluyendoMismoTurno = await _turnoRepository.ExisteSuperposicionAsync(_vetId, fechaHoraBase, turnoEnAtencion.Id);

        // Assert
        Assert.True(superponeMismaHora);
        Assert.True(superponeDentroDe30Min);
        Assert.False(superponeJustoEnBorde30Min);
        Assert.False(superponeExcluyendoMismoTurno);
    }

    [Theory]
    [InlineData("Confirmado", true)]
    [InlineData("EnAtencion", true)]
    [InlineData("Finalizado", true)]
    [InlineData("Pendiente", false)]
    [InlineData("Cancelado", false)]
    public async Task ExisteSuperposicionAsync_RespetaReglasDeDisponibilidadSegunEstado(string estado, bool debeBloquear)
    {
        // Arrange
        var fechaHora = new DateTime(2026, 11, 21, 9, 0, 0, DateTimeKind.Utc);
        var turno = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = fechaHora,
            MotivoConsulta = $"Turno en estado {estado}",
            Estado = estado
        };
        await _turnoRepository.CreateAsync(turno);

        // Act
        var existeSuperposicion = await _turnoRepository.ExisteSuperposicionAsync(_vetId, fechaHora);

        // Assert
        Assert.Equal(debeBloquear, existeSuperposicion);
    }

    [Fact]
    public async Task ExistsByTurnoIdAsync_ConAtencionMedicaVinculada_DevuelveTrue()
    {
        // Arrange
        var turno = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = new DateTime(2026, 11, 22, 10, 0, 0, DateTimeKind.Utc),
            MotivoConsulta = "Control clínico",
            Estado = "EnAtencion"
        };
        await _turnoRepository.CreateAsync(turno);

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            TurnoId = turno.Id,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = "Control clínico",
            Anamnesis = "Paciente estable",
            ExamenFisico = "Mucosas rosadas",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 15.5m,
            EsAptoVacunacion = true
        };
        await _atencionRepository.CreateAsync(atencion);

        // Act
        var existe = await _atencionRepository.ExistsByTurnoIdAsync(turno.Id);

        // Assert
        Assert.True(existe);
    }

    [Fact]
    public async Task ExistsByTurnoIdAsync_SinAtencionMedicaVinculadaOConGuidVacio_DevuelveFalse()
    {
        // Arrange
        var turnoSinAtencion = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = new DateTime(2026, 11, 22, 11, 0, 0, DateTimeKind.Utc),
            MotivoConsulta = "Consulta sin atención aún",
            Estado = "EnAtencion"
        };
        await _turnoRepository.CreateAsync(turnoSinAtencion);

        // Act
        var existeEnTurnoSinAtencion = await _atencionRepository.ExistsByTurnoIdAsync(turnoSinAtencion.Id);
        var existeEnTurnoInexistente = await _atencionRepository.ExistsByTurnoIdAsync(Guid.NewGuid());
        var existeConGuidVacio = await _atencionRepository.ExistsByTurnoIdAsync(Guid.Empty);

        // Assert
        Assert.False(existeEnTurnoSinAtencion);
        Assert.False(existeEnTurnoInexistente);
        Assert.False(existeConGuidVacio);
    }

    [Fact]
    public async Task ExistsByTurnoIdAsync_ConAtencionesEspontaneasSinTurno_NoProduceFalsosPositivos()
    {
        // Arrange
        var turnoSinAtencion = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = new DateTime(2026, 11, 23, 10, 0, 0, DateTimeKind.Utc),
            MotivoConsulta = "Turno programado",
            Estado = "EnAtencion"
        };
        await _turnoRepository.CreateAsync(turnoSinAtencion);

        var atencionEspontanea1 = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            TurnoId = null,
            Fecha = DateTime.UtcNow.AddMinutes(-10),
            MotivoConsulta = "Urgencia espontánea 1",
            Anamnesis = "Anamnesis 1",
            ExamenFisico = "Examen 1",
            Diagnostico = "Diagnóstico 1",
            Tratamiento = "Tratamiento 1",
            PesoRegistrado = 15.2m,
            EsAptoVacunacion = false
        };

        var atencionEspontanea2 = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            TurnoId = null,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = "Urgencia espontánea 2",
            Anamnesis = "Anamnesis 2",
            ExamenFisico = "Examen 2",
            Diagnostico = "Diagnóstico 2",
            Tratamiento = "Tratamiento 2",
            PesoRegistrado = 15.3m,
            EsAptoVacunacion = true
        };

        await _atencionRepository.CreateAsync(atencionEspontanea1);
        await _atencionRepository.CreateAsync(atencionEspontanea2);

        // Act
        var existeParaTurno = await _atencionRepository.ExistsByTurnoIdAsync(turnoSinAtencion.Id);

        // Assert
        Assert.False(existeParaTurno);
    }

    [Fact]
    public async Task RelacionTurnoConsultaYSeguimientoCronico_SeMantienenIndependientesSinConflicto()
    {
        // Arrange: Turno de consulta original en el que se registra la atención médica
        var turnoConsultaOriginal = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = new DateTime(2026, 11, 24, 9, 0, 0, DateTimeKind.Utc),
            MotivoConsulta = "Consulta inicial crónica",
            Estado = "EnAtencion",
            EsAutomatico = false,
            TipoOrigen = "Manual"
        };
        await _turnoRepository.CreateAsync(turnoConsultaOriginal);

        var atencionOrigen = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            TurnoId = turnoConsultaOriginal.Id,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = "Consulta inicial crónica",
            Anamnesis = "Polidipsia",
            ExamenFisico = "Normal",
            Diagnostico = "Enfermedad renal crónica",
            Tratamiento = "Dieta renal y control mensual",
            PesoRegistrado = 14.8m,
            EsAptoVacunacion = false
        };
        await _atencionRepository.CreateAsync(atencionOrigen);

        // Turno futuro de seguimiento crónico originado por la atención (usa Turno.AtencionMedicaId)
        var turnoSeguimiento = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            AtencionMedicaId = atencionOrigen.Id,
            FechaHora = new DateTime(2026, 12, 24, 9, 0, 0, DateTimeKind.Utc),
            MotivoConsulta = "Seguimiento crónico control 1",
            Estado = "Pendiente",
            EsAutomatico = true,
            TipoOrigen = "SeguimientoCronico"
        };
        await _turnoRepository.CreateAsync(turnoSeguimiento);

        // Act
        var turnoOriginalTieneAtencion = await _atencionRepository.ExistsByTurnoIdAsync(turnoConsultaOriginal.Id);
        var turnoSeguimientoTieneAtencionPropia = await _atencionRepository.ExistsByTurnoIdAsync(turnoSeguimiento.Id);
        var atencionTieneSeguimientosEnRepoAtencion = await _atencionRepository.HasTurnosSeguimientoAsync(atencionOrigen.Id);
        var atencionTieneSeguimientosEnRepoTurno = await _turnoRepository.ExisteSeguimientoParaAtencionAsync(atencionOrigen.Id);

        // Assert
        Assert.True(turnoOriginalTieneAtencion);
        Assert.False(turnoSeguimientoTieneAtencionPropia);
        Assert.True(atencionTieneSeguimientosEnRepoAtencion);
        Assert.True(atencionTieneSeguimientosEnRepoTurno);
    }

    [Fact]
    public async Task FlujoCompletoServicioConSQLite_IniciarRegistrarAtencionYFinalizar_PersisteEstadosYTiemposCorrectamente()
    {
        // Arrange
        var dispRepo = new DisponibilidadVeterinarioRepository(_context);
        var mascotaRepo = new MascotaRepository(_context);
        var usuarioRepo = new UsuarioRepository(_context);
        var turnoService = new TurnoService(_turnoRepository, dispRepo, mascotaRepo, usuarioRepo, _atencionRepository);
        var atencionService = new AtencionMedicaService(_atencionRepository, mascotaRepo, turnoService, _turnoRepository, usuarioRepo);

        var turno = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            MotivoConsulta = "Consulta clínica con turno",
            Estado = "Confirmado"
        };
        await _turnoRepository.CreateAsync(turno);

        // Act 1: Iniciar atención (Confirmado -> EnAtencion)
        var iniciado = await turnoService.IniciarAtencionAsync(turno.Id, _vetId, "vet.repo");
        Assert.Equal("EnAtencion", iniciado.Estado);
        Assert.NotNull(iniciado.FechaHoraInicioAtencion);
        Assert.Null(iniciado.FechaHoraFinAtencion);

        // Intentar finalizar antes de registrar atención médica -> debe fallar con TurnoSinAtencionMedicaException
        await Assert.ThrowsAsync<TurnoSinAtencionMedicaException>(() =>
            turnoService.FinalizarAsync(turno.Id, _vetId, "vet.repo"));

        // Act 2: Registrar atención médica vinculada al turno en EnAtencion
        var atencionCreada = await atencionService.CreateAsync(new AtencionMedicaCreateDTO
        {
            MascotaId = _mascotaId,
            TurnoId = turno.Id,
            MotivoConsulta = "Consulta clínica con turno",
            Anamnesis = "Control general",
            ExamenFisico = "Sin particularidades",
            Diagnostico = "Paciente sano",
            Tratamiento = "Control anual",
            PesoRegistrado = 15.6m
        }, _vetId, "vet.repo");

        Assert.Equal(turno.Id, atencionCreada.TurnoId);

        // Intentar registrar una segunda atención para el mismo turno -> debe fallar con TurnoYaTieneAtencionException
        await Assert.ThrowsAsync<TurnoYaTieneAtencionException>(() =>
            atencionService.CreateAsync(new AtencionMedicaCreateDTO
            {
                MascotaId = _mascotaId,
                TurnoId = turno.Id,
                MotivoConsulta = "Intento duplicado",
                Anamnesis = "A",
                ExamenFisico = "E",
                Diagnostico = "D",
                Tratamiento = "T",
                PesoRegistrado = 15.6m
            }, _vetId, "vet.repo"));

        // Act 3: Finalizar turno (EnAtencion -> Finalizado)
        var finalizado = await turnoService.FinalizarAsync(turno.Id, _vetId, "vet.repo");
        Assert.Equal("Finalizado", finalizado.Estado);
        Assert.NotNull(finalizado.FechaHoraInicioAtencion);
        Assert.NotNull(finalizado.FechaHoraFinAtencion);
        Assert.True(finalizado.FechaHoraFinAtencion >= finalizado.FechaHoraInicioAtencion);
    }

    [Fact]
    public async Task TransaccionSQLite_FalloEnSeguimientoCronicoDeAtencionVinculadaATurno_RevierteAtencionYMascotaPermitiendoReintento()
    {
        // Arrange
        var mascotaRepo = new MascotaRepository(_context);
        var usuarioRepo = new UsuarioRepository(_context);
        var turnoServiceFallidoMock = new Mock<ITurnoService>();
        turnoServiceFallidoMock.Setup(s => s.GetDisponibilidadVeterinarioAsync(_vetId))
            .ReturnsAsync(new List<DisponibilidadVeterinarioDTO> { new DisponibilidadVeterinarioDTO() });
        turnoServiceFallidoMock.Setup(s => s.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
            .ThrowsAsync(new InvalidOperationException("Fallo en generación de seguimiento crónico"));

        var atencionService = new AtencionMedicaService(
            _atencionRepository,
            mascotaRepo,
            turnoServiceFallidoMock.Object,
            _turnoRepository,
            usuarioRepo);

        var turno = new Turno
        {
            Id = Guid.NewGuid(),
            MascotaId = _mascotaId,
            VeterinarioId = _vetId,
            FechaHora = DateTime.UtcNow.AddHours(1),
            MotivoConsulta = "Consulta crónica",
            Estado = "EnAtencion",
            FechaHoraInicioAtencion = DateTime.UtcNow.AddMinutes(-5)
        };
        await _turnoRepository.CreateAsync(turno);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = _mascotaId,
            TurnoId = turno.Id,
            MotivoConsulta = "Consulta crónica",
            Anamnesis = "Anamnesis",
            ExamenFisico = "Examen",
            Diagnostico = "Insuficiencia",
            Tratamiento = "Medicación",
            PesoRegistrado = 15.0m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act & Assert: Falla la generación de seguimientos y hace rollback completo en SQLite
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            atencionService.CreateAsync(dto, _vetId, "vet.repo"));

        // Verificar que la atención NO quedó persistida en SQLite y el turno sigue libre de atención vinculada
        Assert.False(await _atencionRepository.ExistsByTurnoIdAsync(turno.Id));
        var mascotaDespuesFallo = await mascotaRepo.GetByIdAsync(_mascotaId);
        Assert.Null(mascotaDespuesFallo!.FechaUltimaVisita);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

