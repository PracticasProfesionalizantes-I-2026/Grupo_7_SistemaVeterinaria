using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.Mascota;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class MascotaServiceTests
{
    private readonly Mock<IMascotaRepository> _mascotaRepoMock;
    private readonly Mock<IDuenoRepository> _duenoRepoMock;
    private readonly MascotaService _service;

    public MascotaServiceTests()
    {
        _mascotaRepoMock = new Mock<IMascotaRepository>();
        _duenoRepoMock = new Mock<IDuenoRepository>();
        _service = new MascotaService(_mascotaRepoMock.Object, _duenoRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_DuenoInexistente_LanzaDuenoNotFoundException()
    {
        // Arrange (RN-06)
        var duenoId = Guid.NewGuid();
        var dto = new MascotaCreateDTO
        {
            DuenoId = duenoId,
            Nombre = "Bobby",
            Especie = "Perro",
            Raza = "Mestizo",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Peso = 12.5m
        };

        _duenoRepoMock.Setup(r => r.GetByIdAsync(duenoId)).ReturnsAsync((Dueno?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DuenoNotFoundException>(() => _service.CreateAsync(dto));
        _mascotaRepoMock.Verify(r => r.CreateAsync(It.IsAny<Mascota>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_RetornaResponseDTO()
    {
        // Arrange
        var duenoId = Guid.NewGuid();
        var dueno = new Dueno { Id = duenoId, Nombre = "María", Apellido = "Gómez" };
        var dto = new MascotaCreateDTO
        {
            DuenoId = duenoId,
            Nombre = "Simba",
            Especie = "Gato",
            Raza = "Común",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 4.5m
        };

        _duenoRepoMock.Setup(r => r.GetByIdAsync(duenoId)).ReturnsAsync(dueno);

        var nuevoId = Guid.NewGuid();
        _mascotaRepoMock.Setup(r => r.CreateAsync(It.IsAny<Mascota>()))
                        .ReturnsAsync((Mascota m) =>
                        {
                            m.Id = nuevoId;
                            return m;
                        });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nuevoId, result.Id);
        Assert.Equal("Simba", result.Nombre);
        Assert.True(result.Activa);
        Assert.Equal("María Gómez", result.NombreDueno);
        _mascotaRepoMock.Verify(r => r.CreateAsync(It.IsAny<Mascota>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PesoInvalido_LanzaValidationException()
    {
        // Arrange
        var dto = new MascotaCreateDTO
        {
            DuenoId = Guid.NewGuid(),
            Nombre = "Luna",
            Especie = "Perro",
            Raza = "Poodle",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 0 // Invalido
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task DesactivarAsync_MascotaActiva_CambiaEstadoAInactivaYRetornaDTO()
    {
        // Arrange (RN-07 / MEJ-03: Baja lógica exitosa -> 200 OK)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Firulais",
            Activa = true
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);
        _mascotaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Mascota>()))
                        .ReturnsAsync((Mascota m) => m);

        // Act
        var result = await _service.DesactivarAsync(mascotaId);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Activa);
        _mascotaRepoMock.Verify(r => r.UpdateAsync(It.Is<Mascota>(m => !m.Activa)), Times.Once);
    }

    [Fact]
    public async Task DesactivarAsync_MascotaYaInactiva_LanzaMascotaYaInactivaException()
    {
        // Arrange (RN-07 / MEJ-03: Mascota previamente inactiva -> 409 Conflict)
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            Nombre = "Rocky",
            Activa = false // Ya inactiva
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaYaInactivaException>(() => _service.DesactivarAsync(mascotaId));
        _mascotaRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Mascota>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_MascotaInexistente_LanzaMascotaNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mascotaRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Mascota?)null);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaNotFoundException>(() => _service.GetByIdAsync(id));
    }
}
