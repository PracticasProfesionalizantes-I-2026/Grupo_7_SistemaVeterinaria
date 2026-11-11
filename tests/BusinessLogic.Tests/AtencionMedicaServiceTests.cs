using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Shared.DTOs.AtencionMedica;
using Shared.DTOs.Turno;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class AtencionMedicaServiceTests
{
    private readonly Mock<IAtencionMedicaRepository> _atencionRepoMock;
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly Mock<ITurnoService> _turnoServiceMock;
    private readonly AtencionMedicaService _service;

    public AtencionMedicaServiceTests()
    {
        _atencionRepoMock = new Mock<IAtencionMedicaRepository>();
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _turnoServiceMock = new Mock<ITurnoService>();
        _turnoServiceMock.Setup(t => t.GetDisponibilidadVeterinarioAsync(It.IsAny<Guid>()))
                         .ReturnsAsync(new List<DisponibilidadVeterinarioDTO> { new DisponibilidadVeterinarioDTO() });
        _service = new AtencionMedicaService(_atencionRepoMock.Object, _mascotaRepoMock.Object, _turnoServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_MascotaInexistente_LanzaMascotaNotFoundException()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Consulta general",
            Anamnesis = "Anamnesis de control",
            ExamenFisico = "Examen físico normal",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 10m
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync((Mascota?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaNotFoundException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_MascotaInactiva_LanzaMascotaInactivaException()
    {
        // Arrange (RN-07: Mascota inactiva no puede recibir atención médica)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Rocky", Activa = false };

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación",
            Anamnesis = "Anamnesis de control",
            ExamenFisico = "Examen físico normal",
            Diagnostico = "Control",
            Tratamiento = "Vacuna",
            PesoRegistrado = 25m
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaInactivaException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(0.04)]
    [InlineData(200.01)]
    [InlineData(500)]
    [InlineData(18.125)]
    public async Task CreateAsync_PesoInvalidoFueraDeRangoOMasDeDosDecimales_LanzaValidationException(decimal pesoInvalido)
    {
        // Arrange (VAL-02: 0.05 kg a 200 kg, máx 2 decimales)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Anamnesis de control",
            ExamenFisico = "Examen físico normal",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = pesoInvalido
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("peso registrado", ex.Message, StringComparison.OrdinalIgnoreCase);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(18.25)]
    [InlineData(200.00)]
    public async Task CreateAsync_PesoRegistradoEnLimitesYConDosDecimales_PermiteCreacion(decimal pesoValido)
    {
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = pesoValido };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control de peso",
            Anamnesis = "Control general",
            ExamenFisico = "Constantes normales",
            Diagnostico = "Sano",
            Tratamiento = "Control",
            PesoRegistrado = pesoValido
        };

        var result = await _service.CreateAsync(dto);

        Assert.NotNull(result);
        Assert.Equal(pesoValido, result.PesoRegistrado);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.Is<AtencionMedica>(a => a.PesoRegistrado == pesoValido)), Times.Once);
    }

    [Theory]
    [InlineData("", "Anamnesis", "Examen físico", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "", "Examen físico", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "   ", "Examen físico", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", null, "Examen físico", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "Anamnesis", "", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "Anamnesis", "   ", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "Anamnesis", null, "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "Anamnesis", "Examen físico", "", "Tratamiento")]
    [InlineData("Motivo", "Anamnesis", "Examen físico", "Diagnóstico", "")]
    public async Task CreateAsync_CamposObligatoriosVacios_LanzaValidationException(
        string motivo, string? anamnesis, string? examenFisico, string diag, string trat)
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = motivo,
            Anamnesis = anamnesis!,
            ExamenFisico = examenFisico!,
            Diagnostico = diag,
            Tratamiento = trat,
            PesoRegistrado = 15m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_AnamnesisYExamenFisico_SePersistenExactamenteSinTextosInventados()
    {
        // Arrange (BUG-03 / CU-04: No se generan textos clínicos automáticos)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        AtencionMedica? atencionCapturada = null;
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .Callback<AtencionMedica>(a => atencionCapturada = a)
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control gastroenterológico",
            Anamnesis = "  Vómitos aislados tras cambio de alimento hace 48 horas  ",
            ExamenFisico = "  Mucosas rosadas, abdomen blando e indoloro, T° 38.5°C  ",
            Diagnostico = "Gastritis aguda leve",
            Tratamiento = "Dieta blanda fraccionada y protector gástrico",
            PesoRegistrado = 19.8m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(atencionCapturada);
        Assert.Equal("Vómitos aislados tras cambio de alimento hace 48 horas", atencionCapturada!.Anamnesis);
        Assert.Equal("Mucosas rosadas, abdomen blando e indoloro, T° 38.5°C", atencionCapturada.ExamenFisico);
        Assert.Equal("Vómitos aislados tras cambio de alimento hace 48 horas", result.Anamnesis);
        Assert.Equal("Mucosas rosadas, abdomen blando e indoloro, T° 38.5°C", result.ExamenFisico);
        Assert.NotEqual("Evaluación general del paciente.", result.Anamnesis);
        Assert.NotEqual("Constantes vitales dentro de parámetros normales.", result.ExamenFisico);
    }

    [Fact]
    public async Task CreateAsync_ProcedimientoQuirurgicoIncompleto_LanzaValidationException()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Cirugía",
            Anamnesis = "Traumatismo en miembro posterior",
            ExamenFisico = "Crepitación ósea en fémur derecho",
            Diagnostico = "Fractura",
            Tratamiento = "Osteosíntesis",
            PesoRegistrado = 15m,
            ProcedimientoQuirurgico = new ProcedimientoQuirurgicoCreateDTO
            {
                TipoProcedimiento = "", // Vacío
                Descripcion = "Cirugía de fémur"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_PrescripcionIncompleta_LanzaValidationException()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Infección",
            Anamnesis = "Sacudidas de cabeza frecuentes",
            ExamenFisico = "Conducto auditivo eritematoso",
            Diagnostico = "Otitis",
            Tratamiento = "Gotas",
            PesoRegistrado = 15m,
            Prescripciones = new List<PrescripcionCreateDTO>
            {
                new PrescripcionCreateDTO
                {
                    Medicamento = "Amoxicilina",
                    Dosis = "", // Vacío
                    Frecuencia = "Cada 12hs",
                    Duracion = "7 días"
                }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_EstudioIncompleto_LanzaValidationException()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Traumatismo",
            Anamnesis = "Caída desde altura",
            ExamenFisico = "Dolor a la palpación",
            Diagnostico = "Posible fractura",
            Tratamiento = "Reposo",
            PesoRegistrado = 15m,
            Estudios = new List<EstudioComplementarioCreateDTO>
            {
                new EstudioComplementarioCreateDTO { Nombre = "   " } // Vacío
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_AlertaDesnutricion_DisminucionSuperiorAl10PorcientoConAtencionPrevia_GeneraAlerta()
    {
        // Arrange: Peso previo = 20kg, actual = 17kg -> disminución = 3kg (15% > 10%)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var ultimaAtencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            PesoRegistrado = 20.0m,
            Fecha = DateTime.UtcNow.AddMonths(-1)
        };
        _atencionRepoMock.Setup(r => r.GetUltimaAtencionByMascotaIdAsync(mascotaId)).ReturnsAsync(ultimaAtencion);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Pérdida de peso",
            Anamnesis = "Disminución de ingesta en el último mes",
            ExamenFisico = "Condición corporal 2/5",
            Diagnostico = "Inapetencia",
            Tratamiento = "Dieta hipercalórica",
            PesoRegistrado = 17.0m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert (RN-12)
        Assert.True(result.AlertaDesnutricion);
        Assert.Equal(15.00m, result.PorcentajeDisminucionPeso);
        Assert.Equal(20.0m, result.PesoReferenciaUtilizado);
        Assert.False(string.IsNullOrEmpty(result.MensajeAlertaDesnutricion));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.Is<AtencionMedica>(a => a.AlertaDesnutricion && a.PorcentajeDisminucionPeso == 15.00m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AlertaDesnutricion_DisminucionExacta10Porciento_NoGeneraAlerta()
    {
        // Arrange: Peso previo = 20kg, actual = 18kg -> disminución = 2kg (10.0%, no superior estricto al 10%)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var ultimaAtencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            PesoRegistrado = 20.0m,
            Fecha = DateTime.UtcNow.AddMonths(-1)
        };
        _atencionRepoMock.Setup(r => r.GetUltimaAtencionByMascotaIdAsync(mascotaId)).ReturnsAsync(ultimaAtencion);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Sin novedades clínicas",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 18.0m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert (RN-12: solo si es estrictamente superior al 10%)
        Assert.False(result.AlertaDesnutricion);
        Assert.Equal(10.00m, result.PorcentajeDisminucionPeso);
        Assert.Equal(20.0m, result.PesoReferenciaUtilizado);
    }

    [Theory]
    [InlineData(20.0, 20.0)]
    [InlineData(20.0, 25.0)]
    [InlineData(0.10, 15.00)]
    public async Task CreateAsync_AumentoOMantenimientoDePeso_RegistraPorcentajeCeroYSinAlerta(
        decimal pesoReferencia, decimal pesoActual)
    {
        // Arrange (VAL-03: Cuando aumenta o mantiene peso, PorcentajeDisminucionPeso = 0 y sin alerta)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = pesoReferencia };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var ultimaAtencion = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascotaId,
            PesoRegistrado = pesoReferencia,
            Fecha = DateTime.UtcNow.AddMonths(-1)
        };
        _atencionRepoMock.Setup(r => r.GetUltimaAtencionByMascotaIdAsync(mascotaId)).ReturnsAsync(ultimaAtencion);
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control de crecimiento",
            Anamnesis = "Buen apetito y desarrollo normal",
            ExamenFisico = "Condición corporal óptima",
            Diagnostico = "Sano",
            Tratamiento = "Mantener dieta",
            PesoRegistrado = pesoActual
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.AlertaDesnutricion);
        Assert.Equal(0m, result.PorcentajeDisminucionPeso);
        Assert.Equal(pesoReferencia, result.PesoReferenciaUtilizado);
        Assert.Null(result.MensajeAlertaDesnutricion);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.Is<AtencionMedica>(
            a => !a.AlertaDesnutricion && a.PorcentajeDisminucionPeso == 0m)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_AlertaDesnutricion_PrimeraAtencionUsaPesoFichaMascota_GeneraAlerta()
    {
        // Arrange: Sin atención previa, peso en ficha = 10kg, actual = 8kg (disminución = 20% > 10%)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Luna", Activa = true, Peso = 10.0m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.GetUltimaAtencionByMascotaIdAsync(mascotaId)).ReturnsAsync((AtencionMedica?)null);
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Primera consulta",
            Anamnesis = "Pérdida de peso notada por el dueño",
            ExamenFisico = "Bajo peso evidente",
            Diagnostico = "Delgadez",
            Tratamiento = "Vitaminas",
            PesoRegistrado = 8.0m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result.AlertaDesnutricion);
        Assert.Equal(20.00m, result.PorcentajeDisminucionPeso);
        Assert.Equal(10.0m, result.PesoReferenciaUtilizado);
    }

    [Fact]
    public async Task CreateAsync_AlertaDesnutricion_SinPesoAnteriorNiEnFicha_NoCalculaVariacionNiGeneraAlerta()
    {
        // Arrange: Sin atención previa, peso en ficha = 0
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Kira", Activa = true, Peso = 0m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.GetUltimaAtencionByMascotaIdAsync(mascotaId)).ReturnsAsync((AtencionMedica?)null);
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Primera consulta",
            Anamnesis = "Cachorro recién adoptado",
            ExamenFisico = "Constantes normales",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = 5.0m
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.AlertaDesnutricion);
        Assert.Null(result.PorcentajeDisminucionPeso);
        Assert.Null(result.PesoReferenciaUtilizado);
    }

    [Fact]
    public async Task CreateAsync_ChecklistApto_PermiteVacunacion()
    {
        // Arrange (RN-03, RN-04)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación antirrábica",
            Anamnesis = "Paciente activo y con apetito conservado",
            ExamenFisico = "T° 38.4°C, mucosas rosadas, linfonódulos normales",
            Diagnostico = "Apto clínico para vacunar",
            Tratamiento = "Inmunización",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Antirrábica Canina",
                Dosis = "1 ml subcutánea",
                Lote = "LOTE-2026-X",
                FechaProximaDosis = DateTime.UtcNow.AddYears(1)
            }
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result.EsAptoVacunacion);
        Assert.Equal("APTO", result.AptitudVacunacionResultado);
        Assert.NotNull(result.Vacunacion);
        Assert.Equal("Antirrábica Canina", result.Vacunacion.Vacuna);
        Assert.Equal("1 ml subcutánea", result.Vacunacion.Dosis);
        Assert.Equal("LOTE-2026-X", result.Vacunacion.Lote);
        Assert.NotNull(result.Vacunacion.FechaProximaDosis);
    }

    [Fact]
    public async Task CreateAsync_VacunacionSinFechaProximaDosis_PermiteCreacionConProximaDosisNull()
    {
        // Arrange (VAL-06: FechaProximaDosis es opcional)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación dosis única",
            Anamnesis = "Control sanitario",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Apto clínico",
            Tratamiento = "Inmunización",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Antirrábica Canina",
                Dosis = "1 ml",
                Lote = "LOTE-2026-01",
                FechaProximaDosis = null
            }
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result.Vacunacion);
        Assert.Equal("Antirrábica Canina", result.Vacunacion!.Vacuna);
        Assert.Null(result.Vacunacion.FechaProximaDosis);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.Is<AtencionMedica>(a =>
            a.VacunaAplicada == "Antirrábica Canina" &&
            a.VacunaProximaDosis == null)), Times.Once);
    }

    [Theory]
    [InlineData(-365)]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task CreateAsync_FechaProximaDosisPasadaODelDiaActualODefault_LanzaValidationExceptionYSinPersistir(int diasOffset)
    {
        // Arrange (VAL-06: FechaProximaDosis debe ser estrictamente posterior al día actual)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var fechaInvalida = diasOffset == -365
            ? default
            : DateTime.UtcNow.Date.AddDays(diasOffset);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación",
            Anamnesis = "Sin antecedentes patológicos",
            ExamenFisico = "Examen normal",
            Diagnostico = "Sano",
            Tratamiento = "Vacuna",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Quíntuple Canina",
                Lote = "L-100",
                FechaProximaDosis = fechaInvalida
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("próxima dosis", ex.Message, StringComparison.OrdinalIgnoreCase);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_VacunacionIncompletaSinNombreDeVacuna_LanzaValidationExceptionYSinPersistir(string vacunaVacia)
    {
        // Arrange (VAL-06: Datos de vacunación incompletos o incoherentes -> 400)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Control general",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Sano",
            Tratamiento = "Control",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = vacunaVacia,
                Dosis = "1 ml",
                Lote = "LOTE-99",
                FechaProximaDosis = DateTime.UtcNow.Date.AddDays(30)
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("vacuna", ex.Message, StringComparison.OrdinalIgnoreCase);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Theory]
    [InlineData("MotivoConsulta")]
    [InlineData("Anamnesis")]
    [InlineData("ExamenFisico")]
    [InlineData("Diagnostico")]
    [InlineData("Tratamiento")]
    [InlineData("Observaciones")]
    [InlineData("MotivoNoApto")]
    [InlineData("Vacuna")]
    [InlineData("DosisVacuna")]
    [InlineData("LoteVacuna")]
    [InlineData("TipoProcedimiento")]
    [InlineData("DescripcionProcedimiento")]
    [InlineData("ObservacionesProcedimiento")]
    [InlineData("Medicamento")]
    [InlineData("DosisPrescripcion")]
    [InlineData("FrecuenciaPrescripcion")]
    [InlineData("DuracionPrescripcion")]
    [InlineData("NombreEstudio")]
    [InlineData("ResultadoEstudio")]
    public async Task CreateAsync_CamposSuperanMaxLength_LanzaValidationExceptionYSinPersistir(string campoExcedido)
    {
        // Arrange (VAL-06: Validación de MaxLength en servicio para campos clínicos y sub-objetos)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = campoExcedido == "MotivoConsulta" ? new string('M', 251) : "Consulta general",
            Anamnesis = campoExcedido == "Anamnesis" ? new string('A', 2001) : "Anamnesis normal",
            ExamenFisico = campoExcedido == "ExamenFisico" ? new string('E', 2001) : "Examen físico normal",
            Diagnostico = campoExcedido == "Diagnostico" ? new string('D', 1001) : "Diagnóstico clínico",
            Tratamiento = campoExcedido == "Tratamiento" ? new string('T', 1001) : "Tratamiento clínico",
            PesoRegistrado = 20m,
            Observaciones = campoExcedido == "Observaciones" ? new string('O', 501) : "Observaciones normales",
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = true,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true,
                MotivoNoApto = campoExcedido == "MotivoNoApto" ? new string('N', 501) : null
            },
            Vacunacion = (campoExcedido is "Vacuna" or "DosisVacuna" or "LoteVacuna")
                ? new VacunacionCreateDTO
                {
                    Vacuna = campoExcedido == "Vacuna" ? new string('V', 101) : "Antirrábica",
                    Dosis = campoExcedido == "DosisVacuna" ? new string('D', 51) : "1 ml",
                    Lote = campoExcedido == "LoteVacuna" ? new string('L', 51) : "L-01",
                    FechaProximaDosis = DateTime.UtcNow.Date.AddDays(30)
                }
                : null,
            ProcedimientoQuirurgico = (campoExcedido is "TipoProcedimiento" or "DescripcionProcedimiento" or "ObservacionesProcedimiento")
                ? new ProcedimientoQuirurgicoCreateDTO
                {
                    TipoProcedimiento = campoExcedido == "TipoProcedimiento" ? new string('P', 101) : "Sutura",
                    Descripcion = campoExcedido == "DescripcionProcedimiento" ? new string('D', 1001) : "Sutura simple",
                    ObservacionesComplicaciones = campoExcedido == "ObservacionesProcedimiento" ? new string('C', 501) : "Ninguna"
                }
                : null,
            Prescripciones = (campoExcedido is "Medicamento" or "DosisPrescripcion" or "FrecuenciaPrescripcion" or "DuracionPrescripcion")
                ? new List<PrescripcionCreateDTO>
                {
                    new PrescripcionCreateDTO
                    {
                        Medicamento = campoExcedido == "Medicamento" ? new string('M', 151) : "Amoxicilina",
                        Dosis = campoExcedido == "DosisPrescripcion" ? new string('D', 101) : "250 mg",
                        Frecuencia = campoExcedido == "FrecuenciaPrescripcion" ? new string('F', 101) : "Cada 12 hs",
                        Duracion = campoExcedido == "DuracionPrescripcion" ? new string('U', 101) : "7 días"
                    }
                }
                : new List<PrescripcionCreateDTO>(),
            Estudios = (campoExcedido is "NombreEstudio" or "ResultadoEstudio")
                ? new List<EstudioComplementarioCreateDTO>
                {
                    new EstudioComplementarioCreateDTO
                    {
                        Nombre = campoExcedido == "NombreEstudio" ? new string('E', 151) : "Hemograma",
                        Resultado = campoExcedido == "ResultadoEstudio" ? new string('R', 1001) : "Normal"
                    }
                }
                : new List<EstudioComplementarioCreateDTO>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ChecklistNoApto_SinVacuna_RegistraAtencionConMotivoClinico()
    {
        // Arrange (RN-03, RN-04: Si es NO APTO, permite guardar la atención sin vacuna y con el motivo clínico)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control y posible vacuna",
            Anamnesis = "Decaimiento desde ayer",
            ExamenFisico = "Hipertermia 39.8°C",
            Diagnostico = "Cuadro febril e infeccioso",
            Tratamiento = "Antibioticoterapia oral y reposo",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = false, // Fiebre
                BuenEstadoGeneral = false,
                SinEnfermedadInfecciosaAguda = false,
                AutorizacionVeterinaria = false,
                MotivoNoApto = "Fiebre de 39.8°C y decaimiento general"
            }
            // Sin vacuna aplicada
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.EsAptoVacunacion);
        Assert.Equal("NO_APTO", result.AptitudVacunacionResultado);
        Assert.Equal("Fiebre de 39.8°C y decaimiento general", result.MotivoNoAptoVacunacion);
        Assert.Null(result.VacunaAplicada);
        Assert.Null(result.Vacunacion);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ChecklistNoApto_ConIntentoDeVacuna_LanzaPacienteNoAptoVacunacionException()
    {
        // Arrange: Checklist NO APTO pero se intenta registrar una vacuna
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación",
            Anamnesis = "Paciente decaído",
            ExamenFisico = "T° 39.6°C",
            Diagnostico = "Fiebre",
            Tratamiento = "Ninguno",
            PesoRegistrado = 20m,
            EvaluacionAptitud = new EvaluacionAptitudVacunacionCreateDTO
            {
                SinFiebre = false,
                BuenEstadoGeneral = true,
                SinEnfermedadInfecciosaAguda = true,
                AutorizacionVeterinaria = true
            },
            Vacunacion = new VacunacionCreateDTO
            {
                Vacuna = "Quíntuple"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<PacienteNoAptoVacunacionException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_VacunaSinChecklistNiAptitud_LanzaPacienteNoAptoVacunacionException()
    {
        // Arrange: Intento de vacuna con EsAptoVacunacion = false y sin checklist APTO
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación anual",
            Anamnesis = "Letargo de 24 hs",
            ExamenFisico = "Hipertermia",
            Diagnostico = "Fiebre y letargo",
            Tratamiento = "Antipirético",
            PesoRegistrado = 28m,
            EsAptoVacunacion = false,
            VacunaAplicada = "Antirrábica"
        };

        // Act & Assert
        await Assert.ThrowsAsync<PacienteNoAptoVacunacionException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ConProcedimientoQuirurgicoYPrescripcionesYEstudios_RegistraTodoCorrectamente()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 25m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) =>
                         {
                             a.Id = Guid.NewGuid();
                             return a;
                         });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Cirugía programada",
            Anamnesis = "Paciente en ayuno de 12 horas",
            ExamenFisico = "Constantes fisiológicas normales",
            Diagnostico = "Esterilización de rutina",
            Tratamiento = "Orquiectomía bilateral y postoperatorio",
            PesoRegistrado = 25m,
            Observaciones = "Recuperación anestésica favorable",
            ProcedimientoQuirurgico = new ProcedimientoQuirurgicoCreateDTO
            {
                TipoProcedimiento = "Orquiectomía",
                Descripcion = "Esterilización electiva bajo anestesia general",
                ObservacionesComplicaciones = "Sin incidentes ni sangrado relevante"
            },
            Prescripciones = new List<PrescripcionCreateDTO>
            {
                new PrescripcionCreateDTO
                {
                    Medicamento = "Meloxicam 1mg",
                    Dosis = "1 comprimido",
                    Frecuencia = "Cada 24 hs",
                    Duracion = "3 días"
                }
            },
            Estudios = new List<EstudioComplementarioCreateDTO>
            {
                new EstudioComplementarioCreateDTO
                {
                    Nombre = "Hemograma prequirúrgico",
                    Resultado = "Valores dentro de rangos normales"
                }
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ProcedimientoQuirurgico);
        Assert.Equal("Orquiectomía", result.ProcedimientoQuirurgico.TipoProcedimiento);
        Assert.Single(result.Prescripciones);
        Assert.Equal("Meloxicam 1mg", result.Prescripciones[0].Medicamento);
        Assert.Single(result.Estudios);
        Assert.Equal("Hemograma prequirúrgico", result.Estudios[0].Nombre);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_GeneraTurnosYRetornaFechasPendientes()
    {
        // Arrange (RN-11 / CU-06)
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var resultadoCronico = new GenerarTurnosCronicosResponseDTO
        {
            TurnosGenerados = new List<TurnoResponseDTO>
            {
                new TurnoResponseDTO { Id = Guid.NewGuid(), FechaHora = DateTime.UtcNow.AddDays(14) },
                new TurnoResponseDTO { Id = Guid.NewGuid(), FechaHora = DateTime.UtcNow.AddDays(28) }
            },
            FechasPendientes = new List<DateTime>
            {
                DateTime.UtcNow.AddDays(42)
            },
            Mensaje = "Se generaron 2 turnos de seguimiento crónico. 1 fecha requiere coordinación manual."
        };

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ReturnsAsync(resultadoCronico);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Anamnesis = "Polidipsia y poliuria controladas",
            ExamenFisico = "Mucosas húmedas, normohidratado",
            Diagnostico = "Diabetes Mellitus canina (Crónico)",
            Tratamiento = "Insulina NPH y controles periódicos",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Quincenal",
                JornadaPreferida = "Tarde"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TurnosSeguimiento.Count);
        Assert.Single(result.FechasPendientesManual);
        Assert.NotNull(result.MensajeSeguimientoCronico);
        _turnoServiceMock.Verify(t => t.GenerarTurnosSeguimientoCronicoAsync(It.Is<GenerarTurnosCronicosDTO>(g =>
            g.MascotaId == mascotaId &&
            g.Frecuencia == "Quincenal" &&
            g.JornadaPreferida == "Tarde" &&
            g.VeterinarioId == vetId
        )), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_SinDisponibilidad_NoBloqueaGuardadoAtencion()
    {
        // Arrange: Falta total de cupos en agenda
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        var resultadoSinCupo = new GenerarTurnosCronicosResponseDTO
        {
            TurnosGenerados = new List<TurnoResponseDTO>(),
            FechasPendientes = new List<DateTime>
            {
                DateTime.UtcNow.AddDays(30),
                DateTime.UtcNow.AddDays(60)
            },
            Mensaje = "No se encontraron cupos disponibles. 2 fechas requieren coordinación manual."
        };

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ReturnsAsync(resultadoSinCupo);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control cardíaco",
            Anamnesis = "Tolerancia al ejercicio estable",
            ExamenFisico = "Auscultación con soplo grado II/VI",
            Diagnostico = "Cardiopatía dilatada",
            Tratamiento = "Pimobendan",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.TurnosSeguimiento);
        Assert.Equal(2, result.FechasPendientesManual.Count);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_SerieYaGenerada_PrevieneDuplicacion()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        // Simula que ya se generó la serie para esta atención
        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(true);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Control periódico",
            ExamenFisico = "Normal",
            Diagnostico = "Crónico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        _turnoServiceMock.Verify(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_FallaTecnicaEnPersistenciaDeTurnos_RevierteTransaccionYPropagaExcepcion()
    {
        // Arrange: Si la generación o persistencia de turnos lanza un DbUpdateException, la transacción debe revertirse
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var nuevaAtencionId = Guid.NewGuid();
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = nuevaAtencionId; return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(nuevaAtencionId)).ReturnsAsync(false);

        // Simula falla técnica de base de datos durante la persistencia de los turnos en el generador
        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ThrowsAsync(new DbUpdateException("Database disk image is locked / IO Failure", new Exception()));

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Anamnesis = "Control glucémico mensual",
            ExamenFisico = "Buen estado general",
            Diagnostico = "Diabetes Mellitus",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        // Act & Assert: Debe propagar la excepción y revertir la transacción sin dejar guardado parcial
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.CreateAsync(dto, vetId, "vet.carlos"));
        _atencionRepoMock.Verify(r => r.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _atencionRepoMock.Verify(r => r.CommitTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_FallaEnTurnos_RevierteTransaccionYNoPersisteParcialmente()
    {
        // Arrange: Falla durante la generación de turnos debe revertir la transacción de atención médica
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var nuevaAtencionId = Guid.NewGuid();
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = nuevaAtencionId; a.Fecha = DateTime.UtcNow; return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(nuevaAtencionId)).ReturnsAsync(false);

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ThrowsAsync(new DbUpdateException("Database transient error", new Exception()));

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control cardíaco",
            Anamnesis = "Control cardiológico mensual",
            ExamenFisico = "Frecuencia cardíaca normal",
            Diagnostico = "Cardiopatía",
            Tratamiento = "Medicación",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act & Assert: Falla técnica causa Rollback y no Commit
        await Assert.ThrowsAsync<DbUpdateException>(() => _service.CreateAsync(dto, vetId, "vet.carlos"));
        _atencionRepoMock.Verify(r => r.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _atencionRepoMock.Verify(r => r.CommitTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_ErrorDeProgramacionNoRecuperable_NoSeOcultaYPropaga()
    {
        // Arrange: Un error de programación (NullReferenceException) NO debe ser silenciado
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ThrowsAsync(new NullReferenceException("Referencia nula en lógica de turnos"));

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Control periódico",
            ExamenFisico = "Normal",
            Diagnostico = "Crónico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act & Assert: El error de programación escala y no se oculta
        await Assert.ThrowsAsync<NullReferenceException>(() => _service.CreateAsync(dto, vetId, "vet.carlos"));
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_VeterinarioSinDisponibilidadConfigurada_Retorna201ConTodasFechasPendientes()
    {
        // Arrange: El veterinario no tiene turnos/días de atención configurados en el sistema (identificación tipada inequívoca)
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var atencionId = Guid.NewGuid();
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = atencionId; return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(atencionId)).ReturnsAsync(false);

        // Validación tipada: GetDisponibilidadVeterinarioAsync devuelve colección vacía
        _turnoServiceMock.Setup(t => t.GetDisponibilidadVeterinarioAsync(vetId))
                         .ReturnsAsync(new List<DisponibilidadVeterinarioDTO>());

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Anamnesis = "Control mensual de diabetes",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Diabetes Mellitus canina",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert: Atención médica guardada con éxito (201) y todas las fechas marcadas para coordinación manual
        Assert.NotNull(result);
        Assert.Equal(atencionId, result.Id);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once);
        _turnoServiceMock.Verify(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()), Times.Never);
        Assert.Empty(result.TurnosSeguimiento);
        Assert.Equal(6, result.FechasPendientesManual.Count);
        Assert.NotNull(result.MensajeSeguimientoCronico);
        Assert.Contains("Advertencia", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no cuenta con horarios de disponibilidad configurados", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_InvalidOperationException_NoSeCapturaYPropaga()
    {
        // Arrange: InvalidOperationException representa un error inesperado de programación y NO debe capturarse
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ThrowsAsync(new InvalidOperationException("Operación no válida en el dominio de turnos"));

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Anamnesis = "Control periódico",
            ExamenFisico = "Normal",
            Diagnostico = "Crónico",
            Tratamiento = "Tratamiento",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act & Assert: InvalidOperationException se propaga sin ser enmascarada
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(dto, vetId, "vet.carlos"));
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_TimeoutException_RevierteTransaccionYPropagaExcepcion()
    {
        // Arrange: Falla operativa por timeout en subsistema de agenda debe revertir la transacción de atención médica
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var nuevaAtencionId = Guid.NewGuid();
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = nuevaAtencionId; return a; });

        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(nuevaAtencionId)).ReturnsAsync(false);

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ThrowsAsync(new TimeoutException("Timeout al comunicarse con el subsistema de agenda"));

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Anamnesis = "Control bimestral",
            ExamenFisico = "Estable",
            Diagnostico = "Diabetes",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Bimestral",
                JornadaPreferida = "Tarde"
            }
        };

        // Act & Assert: Debe propagar TimeoutException y ejecutar Rollback
        await Assert.ThrowsAsync<TimeoutException>(() => _service.CreateAsync(dto, vetId, "vet.carlos"));
        _atencionRepoMock.Verify(r => r.RollbackTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Once);
        _atencionRepoMock.Verify(r => r.CommitTransactionAsync(It.IsAny<IDbContextTransaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_LanzaAtencionInmutableException()
    {
        // Act & Assert (RN-01: Inmutabilidad de atenciones médicas)
        await Assert.ThrowsAsync<AtencionInmutableException>(() => _service.UpdateAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_LanzaAtencionInmutableException()
    {
        // Act & Assert (RN-01: Inmutabilidad de atenciones médicas)
        await Assert.ThrowsAsync<AtencionInmutableException>(() => _service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_EsEnfermedadCronicaTrue_SinSeguimientoCronico_LanzaValidationException_SinPersistir()
    {
        // Arrange (BUG-06)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control crónico",
            Anamnesis = "Control general",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Cardiopatía crónica",
            Tratamiento = "Medicación diaria",
            PesoRegistrado = 14m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = null
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EsEnfermedadCronicaFalse_ConSeguimientoCronico_LanzaValidationException_SinPersistir()
    {
        // Arrange (BUG-06)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control agudo",
            Anamnesis = "Control general",
            ExamenFisico = "Parámetros normales",
            Diagnostico = "Cuadro agudo leve",
            Tratamiento = "Reposo",
            PesoRegistrado = 14m,
            EsEnfermedadCronica = false,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_EsEnfermedadCronicaTrue_ConSeguimientoYEspaciosExtremos_GeneraTurnosNormalmente()
    {
        // Arrange (BUG-06)
        var mascotaId = Guid.NewGuid();
        var vetId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Toby", Activa = true, Peso = 14m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) => { a.Id = Guid.NewGuid(); return a; });
        _atencionRepoMock.Setup(r => r.HasTurnosSeguimientoAsync(It.IsAny<Guid>())).ReturnsAsync(false);

        _turnoServiceMock.Setup(t => t.GenerarTurnosSeguimientoCronicoAsync(It.IsAny<GenerarTurnosCronicosDTO>()))
                         .ReturnsAsync(new GenerarTurnosCronicosResponseDTO
                         {
                             TurnosGenerados = new List<TurnoResponseDTO>
                             {
                                 new TurnoResponseDTO { Id = Guid.NewGuid(), FechaHora = DateTime.UtcNow.AddDays(30) }
                             },
                             FechasPendientes = new List<DateTime>(),
                             Mensaje = "Turnos generados"
                         });

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Anamnesis = "Control mensual",
            ExamenFisico = "Normal",
            Diagnostico = "Diabetes Mellitus",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            EsEnfermedadCronica = true,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "  Mensual  ",
                JornadaPreferida = "  Tarde  "
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.TurnosSeguimiento);
        _turnoServiceMock.Verify(t => t.GenerarTurnosSeguimientoCronicoAsync(It.Is<GenerarTurnosCronicosDTO>(g =>
            g.Frecuencia == "Mensual" && g.JornadaPreferida == "Tarde")), Times.Once);
    }
}
