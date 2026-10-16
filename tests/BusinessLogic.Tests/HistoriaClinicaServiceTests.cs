using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class HistoriaClinicaServiceTests
{
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly Mock<IAtencionMedicaRepository> _atencionRepoMock;
    private readonly HistoriaClinicaService _service;

    public HistoriaClinicaServiceTests()
    {
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _atencionRepoMock = new Mock<IAtencionMedicaRepository>();
        _service = new HistoriaClinicaService(_mascotaRepoMock.Object, _atencionRepoMock.Object);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_MascotaInexistente_LanzaMascotaNotFoundException()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync((Mascota?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaNotFoundException>(() => _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId));
        _atencionRepoMock.Verify(r => r.GetByMascotaIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_MascotaValidaConAtenciones_RetornaHistoriaClinicaCompleta()
    {
        // Arrange
        var duenoId = Guid.NewGuid();
        var dueno = new Dueno
        {
            Id = duenoId,
            Nombre = "Carlos",
            Apellido = "González",
            Dni = "34567890",
            Telefono = "11-4567-8901",
            Email = "carlos@example.com"
        };

        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Milo",
            Especie = "Perro",
            Raza = "Golden Retriever",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-3),
            Peso = 28.5m,
            Activa = true,
            Observaciones = "Paciente dócil",
            DuenoId = duenoId,
            Dueno = dueno,
            FechaUltimaVisita = DateTime.UtcNow.AddDays(-5)
        };

        var vetId = Guid.NewGuid();
        var vet = new Usuario { Id = vetId, Username = "vet.carlos" };

        var atencion1 = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            Mascota = mascota,
            VeterinarioId = vetId,
            Veterinario = vet,
            Fecha = DateTime.UtcNow.AddDays(-5),
            MotivoConsulta = "Control anual",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 28.5m,
            EsAptoVacunacion = true,
            VacunaAplicada = "Antirrábica",
            VacunaDosis = "1 ml",
            VacunaLote = "VAC-2026-001"
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica> { atencion1 });

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(mascotaId, result.MascotaId);
        Assert.Equal("Milo", result.NombreMascota);
        Assert.Equal("Carlos González", result.DuenoNombreCompleto);
        Assert.Equal("34567890", result.DuenoDni);
        Assert.True(result.Activa);
        Assert.NotNull(result.FechaUltimaVisita);
        Assert.NotEqual("Sin visitas registradas", result.UltimaVisitaVisualizada);
        Assert.Equal(1, result.TotalAtenciones);
        Assert.Equal(1, result.TotalVacunasAplicadas);
        Assert.Equal(0, result.TotalCirugias);
        Assert.Null(result.MensajeInformativo);
        Assert.Single(result.AtencionesMedicas);
        Assert.NotNull(result.AtencionesMedicas.First().Vacunacion);
        Assert.Equal("Antirrábica", result.AtencionesMedicas.First().Vacunacion!.Vacuna);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_MascotaSinAtenciones_RetornaSinVisitasRegistradasYMensajeInformativo()
    {
        // Arrange (Flujo 3b / INF-07)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Luna",
            Especie = "Gato",
            Raza = "Siamés",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 3.5m,
            Activa = true,
            FechaUltimaVisita = null,
            Dueno = new Dueno { Nombre = "Mariana", Apellido = "López", Dni = "38123456" }
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica>());

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Luna", result.NombreMascota);
        Assert.Null(result.FechaUltimaVisita);
        Assert.Equal("Sin visitas registradas", result.UltimaVisitaVisualizada);
        Assert.Empty(result.AtencionesMedicas);
        Assert.Equal(0, result.TotalAtenciones);
        Assert.Equal("Sin atenciones médicas registradas a la fecha.", result.MensajeInformativo);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_MascotaInactiva_PermiteConsultaYRetornaDatosCorrectos()
    {
        // Arrange (RN-07: Consulta de solo lectura permitida para mascotas inactivas)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Rocky",
            Especie = "Perro",
            Raza = "Boxer",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-5),
            Peso = 32.0m,
            Activa = false, // Inactiva
            Observaciones = "Baja lógica por mudanza",
            FechaUltimaVisita = DateTime.UtcNow.AddMonths(-2),
            Dueno = new Dueno { Nombre = "Mariana", Apellido = "López", Dni = "38123456" }
        };

        var atencionPrevia = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            Fecha = DateTime.UtcNow.AddMonths(-2),
            MotivoConsulta = "Consulta previa a mudanza",
            Diagnostico = "Estado general bueno",
            Tratamiento = "Ninguno",
            PesoRegistrado = 32.0m
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica> { atencionPrevia });

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Activa);
        Assert.Equal("Rocky", result.NombreMascota);
        Assert.Single(result.AtencionesMedicas);
        Assert.NotNull(result.FechaUltimaVisita);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_ConProcedimientoQuirurgico_MapeaDetallesQuirurgicos()
    {
        // Arrange (INF-06)
        var mascotaId = Guid.NewGuid();
        var cirujanoId = Guid.NewGuid();
        var cirujano = new Usuario { Id = cirujanoId, Username = "cirujano.vet" };

        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Simba",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Activa = true
        };

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            Fecha = DateTime.UtcNow.AddDays(-1),
            MotivoConsulta = "Cirugía programada",
            Diagnostico = "Cuerpo extraño gástrico",
            Tratamiento = "Gastrotomía exploratoria",
            PesoRegistrado = 14.0m,
            ProcedimientoQuirurgico = new ProcedimientoQuirurgico
            {
                Id = Guid.NewGuid(),
                TipoProcedimiento = "Gastrotomía",
                Descripcion = "Extracción de pelota de goma sin complicaciones",
                Fecha = DateTime.UtcNow.AddDays(-1),
                VeterinarioId = cirujanoId,
                Veterinario = cirujano,
                ObservacionesComplicaciones = "Recuperación anestésica satisfactoria"
            }
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica> { atencion });

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCirugias);
        var atencionDto = Assert.Single(result.AtencionesMedicas);
        Assert.NotNull(atencionDto.ProcedimientoQuirurgico);
        Assert.Equal("Gastrotomía", atencionDto.ProcedimientoQuirurgico!.TipoProcedimiento);
        Assert.Equal("cirujano.vet", atencionDto.ProcedimientoQuirurgico.VeterinarioNombre);
        Assert.Equal("Recuperación anestésica satisfactoria", atencionDto.ProcedimientoQuirurgico.ObservacionesComplicaciones);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_ConPrescripcionesYEstudios_MapeaColeccionesCompletas()
    {
        // Arrange (RN-02)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Thor", FechaNacimiento = DateTime.UtcNow.AddYears(-4), Activa = true };

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            Fecha = DateTime.UtcNow.AddDays(-3),
            MotivoConsulta = "Infección respiratoria",
            Diagnostico = "Traqueobronquitis",
            Tratamiento = "Antibioticoterapia",
            PesoRegistrado = 20.0m,
            Prescripciones = new List<PrescripcionMedica>
            {
                new PrescripcionMedica { Id = Guid.NewGuid(), Medicamento = "Amoxicilina + Ác. Clavulánico", Dosis = "250mg", Frecuencia = "Cada 12hs", Duracion = "10 días" }
            },
            EstudiosComplementarios = new List<EstudioComplementario>
            {
                new EstudioComplementario { Id = Guid.NewGuid(), Nombre = "Radiografía de tórax", Resultado = "Patrón bronquial difuso" }
            }
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica> { atencion });

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        var atencionDto = Assert.Single(result.AtencionesMedicas);
        Assert.Single(atencionDto.Prescripciones);
        Assert.Equal("Amoxicilina + Ác. Clavulánico", atencionDto.Prescripciones[0].Medicamento);
        Assert.Single(atencionDto.Estudios);
        Assert.Equal("Radiografía de tórax", atencionDto.Estudios[0].Nombre);
    }

    [Fact]
    public async Task GetHistoriaClinicaByMascotaIdAsync_ConAlertaDesnutricion_MapeaIndicadoresClinicos()
    {
        // Arrange (RN-12)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Pelusa", FechaNacimiento = DateTime.UtcNow.AddYears(-2), Activa = true };

        var atencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            Fecha = DateTime.UtcNow.AddDays(-2),
            MotivoConsulta = "Pérdida de peso progresiva",
            Diagnostico = "Desnutrición moderada",
            Tratamiento = "Plan nutricional hipercalórico",
            PesoRegistrado = 7.5m,
            AlertaDesnutricion = true,
            PorcentajeDisminucionPeso = 16.67m,
            PesoReferenciaUtilizado = 9.0m
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.GetByMascotaIdAsync(mascotaId)).ReturnsAsync(new List<AtencionMedica> { atencion });

        // Act
        var result = await _service.GetHistoriaClinicaByMascotaIdAsync(mascotaId);

        // Assert
        var atencionDto = Assert.Single(result.AtencionesMedicas);
        Assert.True(atencionDto.AlertaDesnutricion);
        Assert.Equal(16.67m, atencionDto.PorcentajeDisminucionPeso);
        Assert.Equal(9.0m, atencionDto.PesoReferenciaUtilizado);
    }

    [Fact]
    public async Task BuscarHistoriasClinicasAsync_ConCriterioValido_RetornaMascotasFiltradas()
    {
        // Arrange
        var dueno = new Dueno { Id = Guid.NewGuid(), Nombre = "Carlos", Apellido = "González", Dni = "34567890" };
        var mascota1 = new Mascota { Id = Guid.NewGuid(), Nombre = "Milo", Especie = "Perro", Dueno = dueno, FechaUltimaVisita = DateTime.UtcNow.AddDays(-10) };

        _mascotaRepoMock.Setup(r => r.BuscarAsync("Milo")).ReturnsAsync(new List<Mascota> { mascota1 });

        // Act
        var result = await _service.BuscarHistoriasClinicasAsync("  Milo  ");

        // Assert
        var list = Assert.Single(result);
        Assert.Equal("Milo", list.NombreMascota);
        Assert.Equal("Carlos González", list.DuenoNombreCompleto);
        _mascotaRepoMock.Verify(r => r.BuscarAsync("Milo"), Times.Once);
    }

    [Fact]
    public async Task BuscarHistoriasClinicasAsync_DuenoConMultiplesMascotas_RetornaTodasLasMascotasDelDueno()
    {
        // Arrange: Carlos González tiene dos mascotas: Milo y Luna
        var dueno = new Dueno { Id = Guid.NewGuid(), Nombre = "Carlos", Apellido = "González", Dni = "34567890" };
        var mascota1 = new Mascota { Id = Guid.NewGuid(), Nombre = "Milo", Especie = "Perro", Dueno = dueno };
        var mascota2 = new Mascota { Id = Guid.NewGuid(), Nombre = "Luna", Especie = "Gato", Dueno = dueno };

        _mascotaRepoMock.Setup(r => r.BuscarAsync("34567890")).ReturnsAsync(new List<Mascota> { mascota1, mascota2 });

        // Act
        var result = (await _service.BuscarHistoriasClinicasAsync("34567890")).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, m => m.NombreMascota == "Milo");
        Assert.Contains(result, m => m.NombreMascota == "Luna");
        Assert.All(result, m => Assert.Equal("Carlos González", m.DuenoNombreCompleto));
    }
}
