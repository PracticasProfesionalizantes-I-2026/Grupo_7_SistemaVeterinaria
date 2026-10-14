using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.DTOs.Dueno;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class DuenoServiceTests
{
    private readonly Mock<IDuenoRepository> _duenoRepoMock;
    private readonly DuenoService _service;

    public DuenoServiceTests()
    {
        _duenoRepoMock = new Mock<IDuenoRepository>();
        _service = new DuenoService(_duenoRepoMock.Object);
    }

    [Fact]
    public async Task CreateAsync_DniDuplicado_LanzaDniDuplicadoException()
    {
        // Arrange (RN-05)
        var dto = new DuenoCreateDTO
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Dni = "30123456",
            Telefono = "11-2233-4455",
            Email = "juan@example.com"
        };

        var duenoExistente = new Dueno { Id = Guid.NewGuid(), Dni = "30123456" };
        _duenoRepoMock.Setup(r => r.GetByDniAsync("30123456"))
                      .ReturnsAsync(duenoExistente);

        // Act & Assert
        await Assert.ThrowsAsync<DniDuplicadoException>(() => _service.CreateAsync(dto));
        _duenoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Dueno>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_DatosValidos_RetornaResponseDTO()
    {
        // Arrange
        var dto = new DuenoCreateDTO
        {
            Nombre = "Ana",
            Apellido = "Martínez",
            Dni = "35987654",
            Telefono = "11-9988-7766",
            Email = "ana@example.com",
            Direccion = "Av. Santa Fe 2000"
        };

        _duenoRepoMock.Setup(r => r.GetByDniAsync("35987654"))
                      .ReturnsAsync((Dueno?)null);

        var nuevoId = Guid.NewGuid();
        _duenoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Dueno>()))
                      .ReturnsAsync((Dueno d) =>
                      {
                          d.Id = nuevoId;
                          return d;
                      });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(nuevoId, result.Id);
        Assert.Equal("Ana", result.Nombre);
        Assert.Equal("35987654", result.Dni);
        _duenoRepoMock.Verify(r => r.CreateAsync(It.IsAny<Dueno>()), Times.Once);
    }

    [Theory]
    [InlineData("", "Pérez", "12345678", "111", "email@test.com")]
    [InlineData("Juan", "", "12345678", "111", "email@test.com")]
    [InlineData("Juan", "Pérez", "", "111", "email@test.com")]
    [InlineData("Juan", "Pérez", "12345678", "", "email@test.com")]
    [InlineData("Juan", "Pérez", "12345678", "111", "")]
    [InlineData("   ", "Pérez", "12345678", "111", "email@test.com")]
    [InlineData("Juan", "   ", "12345678", "111", "email@test.com")]
    [InlineData("Juan", "Pérez", "   ", "111", "email@test.com")]
    public async Task CreateAsync_CamposVaciosOEspacios_LanzaValidationException(
        string nombre, string apellido, string dni, string tel, string email)
    {
        // Arrange (CU-02 Flujo 2a y 2c)
        var dto = new DuenoCreateDTO
        {
            Nombre = nombre,
            Apellido = apellido,
            Dni = dni,
            Telefono = tel,
            Email = email
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
    }

    [Fact]
    public async Task CreateAsync_DniNoNumerico_LanzaValidationException()
    {
        // Arrange (CU-02 Flujo 2b)
        var dto = new DuenoCreateDTO
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Dni = "30A12345",
            Telefono = "11-2233-4455",
            Email = "juan@example.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("numéricos", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_EmailFormatoInvalido_LanzaValidationException()
    {
        // Arrange (CU-02 Flujo 2b)
        var dto = new DuenoCreateDTO
        {
            Nombre = "Juan",
            Apellido = "Pérez",
            Dni = "30123456",
            Telefono = "11-2233-4455",
            Email = "correo-invalido"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto));
        Assert.Contains("correo electrónico", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_TrimsAndSavesDueno()
    {
        // Arrange (CU-02 Flujo principal)
        var dto = new DuenoCreateDTO
        {
            Nombre = "  Juan Carlos  ",
            Apellido = "  Gómez  ",
            Dni = "  38112233  ",
            Telefono = "  11-4455-6677  ",
            Email = "  juan.gomez@test.com  ",
            Direccion = "  Calle Falsa 123  "
        };

        _duenoRepoMock.Setup(r => r.GetByDniAsync("38112233"))
                      .ReturnsAsync((Dueno?)null);

        Dueno? guardado = null;
        _duenoRepoMock.Setup(r => r.CreateAsync(It.IsAny<Dueno>()))
                      .Callback<Dueno>(d => guardado = d)
                      .ReturnsAsync((Dueno d) =>
                      {
                          d.Id = Guid.NewGuid();
                          return d;
                      });

        // Act
        var result = await _service.CreateAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(guardado);
        Assert.Equal("Juan Carlos", guardado.Nombre);
        Assert.Equal("Gómez", guardado.Apellido);
        Assert.Equal("38112233", guardado.Dni);
        Assert.Equal("11-4455-6677", guardado.Telefono);
        Assert.Equal("juan.gomez@test.com", guardado.Email);
        Assert.Equal("Calle Falsa 123", guardado.Direccion);
    }

    [Fact]
    public async Task GetByIdAsync_DuenoInexistente_LanzaDuenoNotFoundException()
    {
        // Arrange
        var id = Guid.NewGuid();
        _duenoRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((Dueno?)null);

        // Act & Assert
        await Assert.ThrowsAsync<DuenoNotFoundException>(() => _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_DuenoConMascotas_LanzaDuenoConMascotasException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dueno = new Dueno { Id = id, Nombre = "Carlos" };
        _duenoRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(dueno);
        _duenoRepoMock.Setup(r => r.HasMascotasAsync(id)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<DuenoConMascotasException>(() => _service.DeleteAsync(id));
        _duenoRepoMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_DuenoSinMascotas_EliminaExitosamente()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dueno = new Dueno { Id = id, Nombre = "Carlos" };
        _duenoRepoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(dueno);
        _duenoRepoMock.Setup(r => r.HasMascotasAsync(id)).ReturnsAsync(false);
        _duenoRepoMock.Setup(r => r.DeleteAsync(id)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(id);

        // Assert
        Assert.True(result);
        _duenoRepoMock.Verify(r => r.DeleteAsync(id), Times.Once);
    }
}
