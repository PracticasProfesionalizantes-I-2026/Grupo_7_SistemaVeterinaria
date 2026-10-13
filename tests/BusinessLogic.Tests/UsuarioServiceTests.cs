using BusinessLogic.Services;
using DataAccess.Entities;
using DataAccess.Repositories;
using Moq;
using Shared.Exceptions;
using Xunit;

namespace BusinessLogic.Tests;

public class UsuarioServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarioRepoMock;
    private readonly UsuarioService _service;

    public UsuarioServiceTests()
    {
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _service = new UsuarioService(_usuarioRepoMock.Object);
    }

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUserListWithStatusAndRole()
    {
        // Arrange (CU-12 Flujo principal)
        var usuarios = new List<Usuario>
        {
            new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@patitas.com",
                Rol = "Administrador",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0
            },
            new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "user.bloqueado",
                Email = "bloqueado@patitas.com",
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = true,
                IntentosFallidos = 5
            },
            new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "user.inactivo",
                Email = "inactivo@patitas.com",
                Rol = "Veterinario",
                Activo = false,
                Bloqueado = false,
                IntentosFallidos = 0
            }
        };

        _usuarioRepoMock.Setup(r => r.GetAllAsync(null, null, null))
            .ReturnsAsync(usuarios);

        // Act
        var result = (await _service.GetAllAsync()).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        var adminDto = result.First(u => u.Username == "admin");
        Assert.Equal("Activo", adminDto.Estado);
        Assert.Equal("Administrador", adminDto.Rol);
        Assert.False(adminDto.PuedeDesbloquear);
        Assert.True(adminDto.PuedeRestablecerPassword);

        var bloqueadoDto = result.First(u => u.Username == "user.bloqueado");
        Assert.Equal("Bloqueado", bloqueadoDto.Estado);
        Assert.True(bloqueadoDto.PuedeDesbloquear);
        Assert.True(bloqueadoDto.PuedeRestablecerPassword);

        var inactivoDto = result.First(u => u.Username == "user.inactivo");
        Assert.Equal("Inactivo", inactivoDto.Estado);
        Assert.False(inactivoDto.PuedeDesbloquear);
        Assert.True(inactivoDto.PuedeRestablecerPassword);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithNoMatchingCriteria_ReturnsEmptyList()
    {
        // Arrange (CU-12 Flujo A1)
        _usuarioRepoMock.Setup(r => r.GetAllAsync("inexistente", null, null))
            .ReturnsAsync(new List<Usuario>());

        // Act
        var result = await _service.GetAllAsync(username: "inexistente");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllUsersAsync_OnDatabaseFailure_ThrowsPersistenceException()
    {
        // Arrange (CU-12 Flujo A3)
        _usuarioRepoMock.Setup(r => r.GetAllAsync(It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .ThrowsAsync(new InvalidOperationException("Falla en la base de datos"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetAllAsync());
    }

    [Fact]
    public async Task GetByIdAsync_WithValidId_ReturnsUserDetails()
    {
        // Arrange (CU-12 Paso 5)
        var userId = Guid.NewGuid();
        var usuario = new Usuario
        {
            Id = userId,
            Username = "vet.carlos",
            Email = "carlos@patitas.com",
            Rol = "Veterinario",
            Activo = true,
            Bloqueado = false,
            IntentosFallidos = 0,
            RequiereCambioPassword = false,
            FechaCreacion = DateTime.UtcNow
        };

        _usuarioRepoMock.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(usuario);

        // Act
        var result = await _service.GetByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("vet.carlos", result.Username);
        Assert.Equal("Veterinario", result.Rol);
        Assert.Equal("Activo", result.Estado);
        Assert.False(result.PuedeDesbloquear);
        Assert.True(result.PuedeRestablecerPassword);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ThrowsUsuarioNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _usuarioRepoMock.Setup(r => r.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Usuario?)null);

        // Act & Assert
        await Assert.ThrowsAsync<UsuarioNotFoundException>(() => _service.GetByIdAsync(nonExistentId));
    }

    [Fact]
    public async Task MapToResponseDTO_PrioritizesInactivoWhenActivoIsFalseEvenIfBloqueadoIsTrue()
    {
        // Arrange: Precedencia explícita: Inactivo tiene prioridad sobre Bloqueado cuando Activo == false
        var usuario = new Usuario
        {
            Id = Guid.NewGuid(),
            Username = "inactivo.bloqueado",
            Activo = false,
            Bloqueado = true,
            Rol = "Recepcionista"
        };

        _usuarioRepoMock.Setup(r => r.GetAllAsync(null, null, null))
            .ReturnsAsync(new List<Usuario> { usuario });

        // Act
        var result = (await _service.GetAllAsync()).First();

        // Assert
        Assert.Equal("Inactivo", result.Estado);
        Assert.False(result.PuedeDesbloquear); // No se puede desbloquear una cuenta inactiva
        Assert.True(result.PuedeRestablecerPassword);
    }

    [Fact]
    public async Task GetAllUsersAsync_WithFilters_PassesArgumentsToRepository()
    {
        // Arrange
        _usuarioRepoMock.Setup(r => r.GetAllAsync("ana", "Recepcionista", "Activo"))
            .ReturnsAsync(new List<Usuario>());

        // Act
        await _service.GetAllAsync("ana", "Recepcionista", "Activo");

        // Assert
        _usuarioRepoMock.Verify(r => r.GetAllAsync("ana", "Recepcionista", "Activo"), Times.Once);
    }
}
