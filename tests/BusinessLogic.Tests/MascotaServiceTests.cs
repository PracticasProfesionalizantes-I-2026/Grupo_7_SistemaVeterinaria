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
            Nombre = "  Simba  ",
            Especie = "  Gato  ",
            Raza = "  Común  ",
            Sexo = "  Macho  ",
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 4.5m,
            Observaciones = "  Observación con espacios  "
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
        Assert.Equal("Gato", result.Especie);
        Assert.Equal("Común", result.Raza);
        Assert.Equal("Macho", result.Sexo);
        Assert.Equal("Observación con espacios", result.Observaciones);
        Assert.True(result.Activa);
        Assert.Equal("María Gómez", result.NombreDueno);
        _mascotaRepoMock.Verify(r => r.CreateAsync(It.Is<Mascota>(m =>
            m.Nombre == "Simba" &&
            m.Especie == "Gato" &&
            m.Raza == "Común" &&
            m.Sexo == "Macho" &&
            m.Observaciones == "Observación con espacios"
        )), Times.Once);
    }

    [Theory]
    [InlineData("", "Perro", "Mestizo", "Macho")]
    [InlineData("   ", "Perro", "Mestizo", "Macho")]
    [InlineData("Bobby", "", "Mestizo", "Macho")]
    [InlineData("Bobby", "Perro", "", "Macho")]
    [InlineData("Bobby", "Perro", "Mestizo", "")]
    public async Task CreateAsync_CamposVaciosOSoloEspacios_LanzaValidationException(string nombre, string especie, string raza, string sexo)
    {
        var dto = new MascotaCreateDTO
        {
            DuenoId = Guid.NewGuid(),
            Nombre = nombre,
            Especie = especie,
            Raza = raza,
            Sexo = sexo,
            FechaNacimiento = DateTime.UtcNow.AddYears(-1),
            Peso = 5.0m
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
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
    public async Task CreateAsync_FechaFutura_LanzaValidationException()
    {
        var dto = new MascotaCreateDTO
        {
            DuenoId = Guid.NewGuid(),
            Nombre = "Luna",
            Especie = "Perro",
            Raza = "Poodle",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddDays(5),
            Peso = 4.0m
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task UpdateAsync_DatosValidos_AplicaTrimYRetornaResponseDTO()
    {
        var mascotaId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            DuenoId = Guid.NewGuid(),
            Nombre = "ViejoNombre",
            Especie = "Perro",
            Raza = "ViejaRaza",
            Sexo = "Macho",
            Peso = 10m,
            Activa = true
        };

        var updateDto = new MascotaUpdateDTO
        {
            Nombre = "  NuevoNombre  ",
            Raza = "  NuevaRaza  ",
            Sexo = "  Hembra  ",
            Peso = 12.5m,
            Observaciones = "  Actualizado  "
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);
        _mascotaRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Mascota>())).ReturnsAsync((Mascota m) => m);

        var result = await _service.UpdateAsync(mascotaId, updateDto);

        Assert.NotNull(result);
        Assert.Equal("NuevoNombre", result.Nombre);
        Assert.Equal("NuevaRaza", result.Raza);
        Assert.Equal("Hembra", result.Sexo);
        Assert.Equal(12.5m, result.Peso);
        Assert.Equal("Actualizado", result.Observaciones);
    }

    [Theory]
    [InlineData("", "Raza", "Macho")]
    [InlineData("   ", "Raza", "Macho")]
    [InlineData("Nombre", "", "Macho")]
    [InlineData("Nombre", "Raza", "")]
    public async Task UpdateAsync_CamposVacios_LanzaValidationException(string nombre, string raza, string sexo)
    {
        var dto = new MascotaUpdateDTO
        {
            Nombre = nombre,
            Raza = raza,
            Sexo = sexo,
            Peso = 10m
        };

        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(Guid.NewGuid(), dto));
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

    // CU-07: Pruebas unitarias de Reasignación de Dueño
    [Fact]
    public async Task ReasignarDuenoAsync_MascotaInexistente_LanzaMascotaNotFoundException()
    {
        var mascotaId = Guid.NewGuid();
        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync((Mascota?)null);

        await Assert.ThrowsAsync<MascotaNotFoundException>(() =>
            _service.ReasignarDuenoAsync(mascotaId, Guid.NewGuid(), Guid.NewGuid(), "recep.ana"));
    }

    [Fact]
    public async Task ReasignarDuenoAsync_MascotaInactiva_LanzaMascotaInactivaException()
    {
        // Arrange (A1: Mascota inactiva -> 409 Conflict)
        var mascotaId = Guid.NewGuid();
        var duenoId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            DuenoId = duenoId,
            Nombre = "Rocky",
            Activa = false
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);

        // Act & Assert
        await Assert.ThrowsAsync<MascotaInactivaException>(() =>
            _service.ReasignarDuenoAsync(mascotaId, Guid.NewGuid(), Guid.NewGuid(), "recep.ana"));
        _mascotaRepoMock.Verify(r => r.ReasignarDuenoConHistorialAsync(It.IsAny<Mascota>(), It.IsAny<HistorialPropietario>()), Times.Never);
    }

    [Fact]
    public async Task ReasignarDuenoAsync_MismoDuenoActual_LanzaValidationException()
    {
        // Arrange (A3: Mismo dueño actual -> 400 Bad Request)
        var mascotaId = Guid.NewGuid();
        var duenoId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            DuenoId = duenoId,
            Nombre = "Milo",
            Activa = true
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() =>
            _service.ReasignarDuenoAsync(mascotaId, duenoId, Guid.NewGuid(), "recep.ana"));
        _mascotaRepoMock.Verify(r => r.ReasignarDuenoConHistorialAsync(It.IsAny<Mascota>(), It.IsAny<HistorialPropietario>()), Times.Never);
    }

    [Fact]
    public async Task ReasignarDuenoAsync_NuevoDuenoInexistente_LanzaDuenoNotFoundException()
    {
        // Arrange (A2: Nuevo dueño inexistente -> 404 Not Found)
        var mascotaId = Guid.NewGuid();
        var duenoActualId = Guid.NewGuid();
        var nuevoDuenoId = Guid.NewGuid();
        var mascota = new Mascota
        {
            Id = mascotaId,
            DuenoId = duenoActualId,
            Nombre = "Milo",
            Activa = true
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);
        _duenoRepoMock.Setup(r => r.GetByIdAsync(nuevoDuenoId)).ReturnsAsync((Dueno?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DuenoNotFoundException>(() =>
            _service.ReasignarDuenoAsync(mascotaId, nuevoDuenoId, Guid.NewGuid(), "recep.ana"));
        _mascotaRepoMock.Verify(r => r.ReasignarDuenoConHistorialAsync(It.IsAny<Mascota>(), It.IsAny<HistorialPropietario>()), Times.Never);
    }

    [Fact]
    public async Task ReasignarDuenoAsync_DatosValidos_ActualizaDuenoYPersisteHistorial()
    {
        // Arrange (Camino feliz de Reasignación -> 200 OK)
        var mascotaId = Guid.NewGuid();
        var duenoActualId = Guid.NewGuid();
        var nuevoDuenoId = Guid.NewGuid();
        var usuarioId = Guid.NewGuid();

        var mascota = new Mascota
        {
            Id = mascotaId,
            DuenoId = duenoActualId,
            Nombre = "Milo",
            Activa = true
        };

        var nuevoDueno = new Dueno
        {
            Id = nuevoDuenoId,
            Nombre = "Juan",
            Apellido = "Pérez"
        };

        _mascotaRepoMock.Setup(r => r.GetByIdTrackedAsync(mascotaId)).ReturnsAsync(mascota);
        _duenoRepoMock.Setup(r => r.GetByIdAsync(nuevoDuenoId)).ReturnsAsync(nuevoDueno);

        HistorialPropietario? historialCapturado = null;
        _mascotaRepoMock.Setup(r => r.ReasignarDuenoConHistorialAsync(It.IsAny<Mascota>(), It.IsAny<HistorialPropietario>()))
                        .Callback<Mascota, HistorialPropietario>((m, h) => historialCapturado = h)
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ReasignarDuenoAsync(mascotaId, nuevoDuenoId, usuarioId, "recep.ana");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nuevoDuenoId, result.DuenoId);
        Assert.Equal("Juan Pérez", result.NombreDueno);

        Assert.NotNull(historialCapturado);
        Assert.Equal(mascotaId, historialCapturado.MascotaId);
        Assert.Equal(duenoActualId, historialCapturado.DuenoAnteriorId);
        Assert.Equal(nuevoDuenoId, historialCapturado.NuevoDuenoId);
        Assert.Equal(usuarioId, historialCapturado.UsuarioId);
        Assert.Equal("recep.ana", historialCapturado.UsuarioUsername);

        _mascotaRepoMock.Verify(r => r.ReasignarDuenoConHistorialAsync(It.IsAny<Mascota>(), It.IsAny<HistorialPropietario>()), Times.Once);
    }
}
