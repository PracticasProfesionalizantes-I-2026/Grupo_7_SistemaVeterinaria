using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.AtencionMedica;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class AtencionMedicaServiceTests
{
    private readonly Mock<IAtencionMedicaRepository> _atencionRepoMock;
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly AtencionMedicaService _service;

    public AtencionMedicaServiceTests()
    {
        _atencionRepoMock = new Mock<IAtencionMedicaRepository>();
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _service = new AtencionMedicaService(_atencionRepoMock.Object, _mascotaRepoMock.Object);
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

    [Fact]
    public async Task CreateAsync_VacunaSinAptitudClinica_LanzaPacienteNoAptoVacunacionException()
    {
        // Arrange (RN-03 y RN-04: Vacuna requiere EsAptoVacunacion == true)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Vacunación anual",
            Diagnostico = "Fiebre y letargo",
            Tratamiento = "Antipirético",
            PesoRegistrado = 28m,
            EsAptoVacunacion = false, // NO APTO
            VacunaAplicada = "Antirrábica" // Intenta aplicar vacuna
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        // Act & Assert
        await Assert.ThrowsAsync<PacienteNoAptoVacunacionException>(() => _service.CreateAsync(dto));
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DatosValidosConVacunaApto_RegistraAtencionYActualizaUltimaVisita()
    {
        // Arrange
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota { Id = mascotaId, Nombre = "Milo", Activa = true };

        var dto = new AtencionMedicaCreateDTO
        {
            MascotaId = mascotaId,
            MotivoConsulta = "Control y vacuna",
            Diagnostico = "Excelente salud clínica",
            Tratamiento = "Vacunación antirrábica aplicada",
            PesoRegistrado = 29.5m,
            EsAptoVacunacion = true,
            VacunaAplicada = "Antirrábica Canina",
            Observaciones = "Próximo control en un año."
        };

        _mascotaRepoMock.Setup(r => r.GetByIdAsync(mascotaId)).ReturnsAsync(mascota);

        var nuevaAtencionId = Guid.NewGuid();
        _atencionRepoMock.Setup(r => r.CreateAsync(It.IsAny<AtencionMedica>()))
                         .ReturnsAsync((AtencionMedica a) =>
                         {
                             a.Id = nuevaAtencionId;
                             return a;
                         });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nuevaAtencionId, result.Id);
        Assert.Equal("Antirrábica Canina", result.VacunaAplicada);
        Assert.True(result.EsAptoVacunacion);

        // Verifica que se haya guardado y actualizado la fecha de última visita
        _atencionRepoMock.Verify(r => r.CreateAsync(It.IsAny<AtencionMedica>()), Times.Once);
        _mascotaRepoMock.Verify(r => r.UpdateUltimaVisitaAsync(mascotaId, It.IsAny<DateTime>()), Times.Once);
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
