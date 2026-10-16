using System.Text.Json;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.Reporte;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class ReporteServiceTests
{
    private readonly Mock<IReporteRepository> _reporteRepoMock;
    private readonly Mock<IAtencionMedicaRepository> _atencionRepoMock;
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly Mock<IDuenoRepository> _duenoRepoMock;
    private readonly Mock<ITurnoRepository> _turnoRepoMock;
    private readonly ReporteService _service;

    public ReporteServiceTests()
    {
        _reporteRepoMock = new Mock<IReporteRepository>();
        _atencionRepoMock = new Mock<IAtencionMedicaRepository>();
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _duenoRepoMock = new Mock<IDuenoRepository>();
        _turnoRepoMock = new Mock<ITurnoRepository>();

        _service = new ReporteService(
            _reporteRepoMock.Object,
            _atencionRepoMock.Object,
            _mascotaRepoMock.Object,
            _duenoRepoMock.Object,
            _turnoRepoMock.Object);
    }

    [Fact]
    public async Task GenerarReporteAsync_ParametrosValidos_GeneraYPersisteReporteConTotalesCorrectos()
    {
        // Arrange
        var usuarioId = Guid.NewGuid();
        var username = "admin";
        var desde = DateTime.UtcNow.AddMonths(-1);
        var hasta = DateTime.UtcNow;

        var request = new GenerarReporteRequestDTO
        {
            Motivo = "Balance general mensual",
            TiposReporte = new List<string> { "Atenciones", "Vacunaciones", "Mascotas", "Duenos", "Turnos" },
            FechaDesde = desde,
            FechaHasta = hasta
        };

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            Fecha = DateTime.UtcNow.AddDays(-10),
            MotivoConsulta = "Control anual",
            Diagnostico = "Sano",
            EsAptoVacunacion = true,
            VacunaAplicada = "Antirrábica",
            AlertaDesnutricion = false
        };

        var mascota = new Mascota { Id = Guid.NewGuid(), Nombre = "Milo", Activa = true, Especie = "Perro", Sexo = "Macho" };
        var dueno = new Dueno { Id = Guid.NewGuid(), Nombre = "Carlos", Apellido = "González", FechaRegistro = DateTime.UtcNow.AddDays(-15) };
        var turno = new Turno { Id = Guid.NewGuid(), FechaHora = DateTime.UtcNow.AddDays(-5), Estado = "Finalizado" };

        _atencionRepoMock.Setup(r => r.GetByRangoFechasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(new List<AtencionMedica> { atencion });
        _mascotaRepoMock.Setup(r => r.GetAllAsync(null))
                        .ReturnsAsync(new List<Mascota> { mascota });
        _duenoRepoMock.Setup(r => r.GetByRangoFechasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                      .ReturnsAsync(new List<Dueno> { dueno });
        _turnoRepoMock.Setup(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null))
                      .ReturnsAsync(new List<Turno> { turno });

        _reporteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reporte>()))
                        .ReturnsAsync((Reporte r) =>
                        {
                            r.Id = Guid.NewGuid();
                            return r;
                        });

        // Act
        var result = await _service.GenerarReporteAsync(request, usuarioId, username);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Balance general mensual", result.Motivo);
        Assert.Equal(username, result.UsuarioGeneradorUsername);
        Assert.Equal(1, result.TotalAtenciones);
        Assert.Equal(1, result.TotalVacunaciones);
        Assert.Equal(1, result.TotalMascotas);
        Assert.Equal(1, result.TotalDuenos);
        Assert.Equal(1, result.TotalTurnos);
        Assert.NotNull(result.DatosReporte.Atenciones);
        Assert.NotNull(result.DatosReporte.Vacunaciones);
        Assert.NotNull(result.DatosReporte.Mascotas);
        Assert.NotNull(result.DatosReporte.Duenos);
        Assert.NotNull(result.DatosReporte.Turnos);

        _reporteRepoMock.Verify(r => r.CreateAsync(It.IsAny<Reporte>()), Times.Once);
    }

    [Fact]
    public async Task GenerarReporteAsync_FechaDesdeMayorAFechaHasta_LanzaRangoFechasInvalidoException()
    {
        // Arrange (CU-11 Flujo alternativo 3a)
        var request = new GenerarReporteRequestDTO
        {
            Motivo = "Reporte con fechas erróneas",
            TiposReporte = new List<string> { "Atenciones" },
            FechaDesde = DateTime.UtcNow.AddDays(5),
            FechaHasta = DateTime.UtcNow
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<RangoFechasInvalidoException>(() =>
            _service.GenerarReporteAsync(request, Guid.NewGuid(), "admin"));

        Assert.Equal("La fecha inicial no puede ser posterior a la fecha final.", ex.Message);
        _reporteRepoMock.Verify(r => r.CreateAsync(It.IsAny<Reporte>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GenerarReporteAsync_SinMotivo_LanzaValidationException(string? motivoInvalido)
    {
        // Arrange (CU-11 Flujo alternativo 2b)
        var request = new GenerarReporteRequestDTO
        {
            Motivo = motivoInvalido!,
            TiposReporte = new List<string> { "Atenciones" },
            FechaDesde = DateTime.UtcNow.AddDays(-5),
            FechaHasta = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GenerarReporteAsync(request, Guid.NewGuid(), "admin"));
    }

    [Fact]
    public async Task GenerarReporteAsync_SinTiposReporte_LanzaValidationException()
    {
        // Arrange (CU-11 Flujo alternativo 2b)
        var request = new GenerarReporteRequestDTO
        {
            Motivo = "Reporte sin módulos",
            TiposReporte = new List<string>(),
            FechaDesde = DateTime.UtcNow.AddDays(-5),
            FechaHasta = DateTime.UtcNow
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.GenerarReporteAsync(request, Guid.NewGuid(), "admin"));
    }

    [Fact]
    public async Task GenerarReporteAsync_SoloAtencionesYVacunaciones_CalculaUnicamenteModulosSeleccionados()
    {
        // Arrange: Sub-variación 1 (reporte modular selectivo)
        var request = new GenerarReporteRequestDTO
        {
            Motivo = "Reporte exclusivo clínico y vacunas",
            TiposReporte = new List<string> { "Atenciones", "Vacunaciones" },
            FechaDesde = DateTime.UtcNow.AddDays(-10),
            FechaHasta = DateTime.UtcNow
        };

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            Fecha = DateTime.UtcNow.AddDays(-3),
            MotivoConsulta = "Vacunación",
            Diagnostico = "Sano",
            EsAptoVacunacion = true,
            VacunaAplicada = "Antirrábica"
        };

        _atencionRepoMock.Setup(r => r.GetByRangoFechasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                         .ReturnsAsync(new List<AtencionMedica> { atencion });

        _reporteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reporte>()))
                        .ReturnsAsync((Reporte r) => r);

        // Act
        var result = await _service.GenerarReporteAsync(request, Guid.NewGuid(), "admin");

        // Assert
        Assert.NotNull(result.DatosReporte.Atenciones);
        Assert.NotNull(result.DatosReporte.Vacunaciones);
        Assert.Null(result.DatosReporte.Mascotas);
        Assert.Null(result.DatosReporte.Duenos);
        Assert.Null(result.DatosReporte.Turnos);
        Assert.Equal(1, result.TotalAtenciones);
        Assert.Equal(1, result.TotalVacunaciones);
        Assert.Equal(0, result.TotalMascotas);
        Assert.Equal(0, result.TotalDuenos);
        Assert.Equal(0, result.TotalTurnos);

        _mascotaRepoMock.Verify(r => r.GetAllAsync(null), Times.Never);
        _duenoRepoMock.Verify(r => r.GetByRangoFechasAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()), Times.Never);
        _turnoRepoMock.Verify(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null), Times.Never);
    }

    [Fact]
    public async Task GenerarReporteAsync_ModuloTurnosConCancelaciones_CalculaAuditoriaDeCancelados()
    {
        // Arrange
        var request = new GenerarReporteRequestDTO
        {
            Motivo = "Auditoría de turnos",
            TiposReporte = new List<string> { "Turnos" },
            FechaDesde = DateTime.UtcNow.AddDays(-30),
            FechaHasta = DateTime.UtcNow
        };

        var turnoCancelado = new Turno
        {
            Id = Guid.NewGuid(),
            FechaHora = DateTime.UtcNow.AddDays(-2),
            Estado = "Cancelado",
            MotivoCancelacion = "Fuerza mayor del propietario",
            FechaCancelacion = DateTime.UtcNow.AddDays(-3),
            UsuarioCancelacionUsername = "recep.ana",
            Mascota = new Mascota { Nombre = "Milo", Dueno = new Dueno { Nombre = "Carlos", Apellido = "González" } },
            Veterinario = new Usuario { Username = "vet.carlos" }
        };

        _turnoRepoMock.Setup(r => r.GetAgendaAsync(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), null, null))
                      .ReturnsAsync(new List<Turno> { turnoCancelado });

        _reporteRepoMock.Setup(r => r.CreateAsync(It.IsAny<Reporte>()))
                        .ReturnsAsync((Reporte r) => r);

        // Act
        var result = await _service.GenerarReporteAsync(request, Guid.NewGuid(), "admin");

        // Assert
        Assert.NotNull(result.DatosReporte.Turnos);
        Assert.Equal(1, result.DatosReporte.Turnos!.TotalTurnos);
        Assert.Equal(1, result.DatosReporte.Turnos.TurnosCancelados);
        Assert.True(result.DatosReporte.Turnos.CancelacionesPorMotivo.ContainsKey("Fuerza mayor del propietario"));
        var detalle = Assert.Single(result.DatosReporte.Turnos.DetalleCancelaciones);
        Assert.Equal("Fuerza mayor del propietario", detalle.MotivoCancelacion);
        Assert.Equal("recep.ana", detalle.UsuarioCancelacionUsername);
        Assert.Equal("Milo", detalle.MascotaNombre);
    }

    [Fact]
    public async Task GetReporteByIdAsync_IdExistente_RetornaReporteResponseDTO()
    {
        // Arrange (CU-10 Flujo Principal)
        var id = Guid.NewGuid();
        var detalle = new ReporteDetalleDTO
        {
            Atenciones = new MetricasAtencionesDTO { TotalAtenciones = 5, TotalCirugias = 1 }
        };

        var reporte = new Reporte
        {
            Id = id,
            Motivo = "Reporte semestral",
            FechaDesde = DateTime.UtcNow.AddMonths(-6),
            FechaHasta = DateTime.UtcNow,
            TiposReporteJson = JsonSerializer.Serialize(new List<string> { "Atenciones" }),
            FechaGeneracion = DateTime.UtcNow,
            UsuarioGeneradorId = Guid.NewGuid(),
            UsuarioGeneradorUsername = "admin",
            TotalAtenciones = 5,
            DatosConsolidadosJson = JsonSerializer.Serialize(detalle)
        };

        _reporteRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(reporte);

        // Act
        var result = await _service.GetReporteByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Reporte semestral", result.Motivo);
        Assert.Equal(5, result.TotalAtenciones);
        Assert.NotNull(result.DatosReporte.Atenciones);
        Assert.Equal(5, result.DatosReporte.Atenciones!.TotalAtenciones);
        Assert.Equal(1, result.DatosReporte.Atenciones.TotalCirugias);
    }

    [Fact]
    public async Task GetReporteByIdAsync_IdInexistente_LanzaReporteNotFoundException()
    {
        // Arrange (CU-10 Flujo alternativo 3a)
        var id = Guid.NewGuid();
        _reporteRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Reporte?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ReporteNotFoundException>(() => _service.GetReporteByIdAsync(id));
    }

    [Fact]
    public async Task GetAllReportesAsync_RetornaHistoricoOrdenadoCronologicamente()
    {
        // Arrange (CU-10 Sub-variación 1)
        var reporte1 = new Reporte
        {
            Id = Guid.NewGuid(),
            Motivo = "Reporte 1",
            FechaGeneracion = DateTime.UtcNow.AddDays(-2),
            TiposReporteJson = "[\"Atenciones\"]",
            TotalAtenciones = 2
        };
        var reporte2 = new Reporte
        {
            Id = Guid.NewGuid(),
            Motivo = "Reporte 2",
            FechaGeneracion = DateTime.UtcNow.AddDays(-1),
            TiposReporteJson = "[\"Vacunaciones\"]",
            TotalVacunaciones = 3
        };

        _reporteRepoMock.Setup(r => r.GetAllAsync())
                        .ReturnsAsync(new List<Reporte> { reporte2, reporte1 });

        // Act
        var result = (await _service.GetAllReportesAsync()).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Reporte 2", result[0].Motivo);
        Assert.Equal("Reporte 1", result[1].Motivo);
    }
}
