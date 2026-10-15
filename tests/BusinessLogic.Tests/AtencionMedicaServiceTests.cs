using BusinessLogic.Interfaces;
using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;
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
    public async Task CreateAsync_PesoInvalidoCeroONegativo_LanzaValidationException(decimal pesoInvalido)
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control",
            Diagnostico = "Sano",
            Tratamiento = "Ninguno",
            PesoRegistrado = pesoInvalido
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("peso registrado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", "Diagnóstico", "Tratamiento")]
    [InlineData("Motivo", "", "Tratamiento")]
    [InlineData("Motivo", "Diagnóstico", "")]
    public async Task CreateAsync_CamposObligatoriosVacios_LanzaValidationException(string motivo, string diag, string trat)
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true, Peso = 20m };
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = motivo,
            Diagnostico = diag,
            Tratamiento = trat,
            PesoRegistrado = 15m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
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
    public async Task CreateAsync_SeguimientoCronico_FallaTecnicaEnPersistenciaDeTurnos_Retorna201ConAdvertenciaYFechasPendientes()
    {
        // Arrange: La atención clínica se guarda con éxito, pero la persistencia de turnos lanza un DbUpdateException
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

        // Al consultar la base de datos tras la falla, no hay turnos persistidos
        _atencionRepoMock.Setup(r => r.GetByIdAsync(nuevaAtencionId)).ReturnsAsync((AtencionMedica?)null);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Diagnostico = "Diabetes Mellitus",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Tarde"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert: No arroja 500, devuelve 201 con la atención guardada y advertencia explícita
        Assert.NotNull(result);
        Assert.Equal(nuevaAtencionId, result.Id);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once); // Atención clínica intacta
        Assert.NotNull(result.MensajeSeguimientoCronico);
        Assert.Contains("Advertencia", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("falla técnica", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(6, result.FechasPendientesManual.Count); // Mensual a 6 meses = 6 fechas pendientes calculadas confiablemente
        Assert.Empty(result.TurnosSeguimiento);
    }

    [Fact]
    public async Task CreateAsync_SeguimientoCronico_FallaTecnicaConTurnosPreviamentePersistidos_ConsultaEstadoRealYNoDuplica()
    {
        // Arrange: Falla técnica pero en la base de datos ya se habían registrado 2 turnos de esa atención
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

        // En la base de datos se comprueba el estado real: se encuentra 1 turno persistido
        var atencionCon1Turno = new AtencionMedica
        {
            Id = nuevaAtencionId,
            MascotaId = mascotaId,
            Mascota = mascota,
            Fecha = DateTime.UtcNow,
            TurnosSeguimiento = new List<Turno>
            {
                new Turno
                {
                    Id = Guid.NewGuid(),
                    MascotaId = mascotaId,
                    VeterinarioId = vetId,
                    AtencionMedicaId = nuevaAtencionId,
                    FechaHora = DateTime.UtcNow.AddMonths(1),
                    Estado = "Pendiente",
                    EsAutomatico = true,
                    TipoOrigen = "SeguimientoCronico"
                }
            }
        };
        _atencionRepoMock.Setup(r => r.GetByIdAsync(nuevaAtencionId)).ReturnsAsync(atencionCon1Turno);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control cardíaco",
            Diagnostico = "Cardiopatía",
            Tratamiento = "Medicación",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Mensual",
                JornadaPreferida = "Manana"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert: Se reporta exactamente el turno real y las fechas restantes sin duplicar
        Assert.NotNull(result);
        Assert.Single(result.TurnosSeguimiento);
        Assert.Equal(5, result.FechasPendientesManual.Count); // 6 ideales - 1 ya asignado = 5 pendientes
        Assert.NotNull(result.MensajeSeguimientoCronico);
        Assert.Contains("Advertencia", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
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
    public async Task CreateAsync_SeguimientoCronico_TimeoutException_Retorna201ConAdvertenciaYFechasPendientes()
    {
        // Arrange: Falla operativa recuperable por timeout en subsistema de agenda
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

        _atencionRepoMock.Setup(r => r.GetByIdAsync(nuevaAtencionId)).ReturnsAsync((AtencionMedica?)null);

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control diabetes",
            Diagnostico = "Diabetes",
            Tratamiento = "Insulina",
            PesoRegistrado = 14m,
            SeguimientoCronico = new SeguimientoCronicoCreateDTO
            {
                Frecuencia = "Bimestral",
                JornadaPreferida = "Tarde"
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, vetId, "vet.carlos");

        // Assert: Atención médica guardada con éxito (201) y advertencia de falla técnica
        Assert.NotNull(result);
        Assert.Equal(nuevaAtencionId, result.Id);
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once);
        Assert.NotNull(result.MensajeSeguimientoCronico);
        Assert.Contains("Advertencia", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("falla técnica", result.MensajeSeguimientoCronico, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, result.FechasPendientesManual.Count);
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
}
