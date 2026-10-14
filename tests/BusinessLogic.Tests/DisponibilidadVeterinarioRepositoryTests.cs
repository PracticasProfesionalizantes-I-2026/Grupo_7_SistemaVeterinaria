using DataAccess.Context;
using DataAccess.Entities;
using DataAccess.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BusinessLogic.Tests;

public class DisponibilidadVeterinarioRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly VeterinariaDbContext _context;
    private readonly DisponibilidadVeterinarioRepository _repository;

    public DisponibilidadVeterinarioRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<VeterinariaDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new VeterinariaDbContext(options);
        _context.Database.EnsureCreated();

        _repository = new DisponibilidadVeterinarioRepository(_context);
    }

    [Fact]
    public async Task GetByVeterinarioIdAsync_ConBaseDatosSQLite_EjecutaSinErrorYOrdenaPorDiaJornadaYHoraInicio()
    {
        // Arrange
        var vetId = Guid.NewGuid();
        var vet = new Usuario
        {
            Id = vetId,
            Username = "vet.test",
            Email = "vet.test@patitas.com",
            PasswordHash = "hash",
            Rol = "Veterinario",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Usuarios.Add(vet);

        // Insertar en orden desordenado a propósito
        var disp1 = new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = DayOfWeek.Wednesday,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(9, 0, 0),
            HoraFin = new TimeSpan(13, 0, 0),
            Activo = true
        };
        var disp2 = new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = DayOfWeek.Monday,
            Jornada = "Tarde",
            HoraInicio = new TimeSpan(14, 0, 0),
            HoraFin = new TimeSpan(18, 0, 0),
            Activo = true
        };
        var disp3 = new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = DayOfWeek.Monday,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 0, 0),
            HoraFin = new TimeSpan(12, 0, 0),
            Activo = true
        };

        _context.DisponibilidadesVeterinarios.AddRange(disp1, disp2, disp3);
        await _context.SaveChangesAsync();

        // Act: En SQLite fallaba con NotSupportedException si el OrderBy TimeSpan se ejecutaba en la base
        var result = (await _repository.GetByVeterinarioIdAsync(vetId)).ToList();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);

        // Primer elemento debe ser Lunes Mañana 08:00
        Assert.Equal(DayOfWeek.Monday, result[0].DiaSemana);
        Assert.Equal("Manana", result[0].Jornada);
        Assert.Equal(new TimeSpan(8, 0, 0), result[0].HoraInicio);

        // Segundo elemento debe ser Lunes Tarde 14:00
        Assert.Equal(DayOfWeek.Monday, result[1].DiaSemana);
        Assert.Equal("Tarde", result[1].Jornada);
        Assert.Equal(new TimeSpan(14, 0, 0), result[1].HoraInicio);

        // Tercer elemento debe ser Miércoles Mañana 09:00
        Assert.Equal(DayOfWeek.Wednesday, result[2].DiaSemana);
        Assert.Equal("Manana", result[2].Jornada);
        Assert.Equal(new TimeSpan(9, 0, 0), result[2].HoraInicio);
    }

    [Fact]
    public async Task GetByVeterinarioAndDiaAsync_ConBaseDatosSQLite_EjecutaSinErrorYOrdenaPorJornadaYHoraInicio()
    {
        // Arrange
        var vetId = Guid.NewGuid();
        var vet = new Usuario
        {
            Id = vetId,
            Username = "vet.test2",
            Email = "vet.test2@patitas.com",
            PasswordHash = "hash",
            Rol = "Veterinario",
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };
        _context.Usuarios.Add(vet);

        var dispTarde = new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = DayOfWeek.Tuesday,
            Jornada = "Tarde",
            HoraInicio = new TimeSpan(14, 0, 0),
            HoraFin = new TimeSpan(18, 0, 0),
            Activo = true
        };
        var dispManana = new DisponibilidadVeterinario
        {
            Id = Guid.NewGuid(),
            VeterinarioId = vetId,
            DiaSemana = DayOfWeek.Tuesday,
            Jornada = "Manana",
            HoraInicio = new TimeSpan(8, 30, 0),
            HoraFin = new TimeSpan(12, 30, 0),
            Activo = true
        };

        _context.DisponibilidadesVeterinarios.AddRange(dispTarde, dispManana);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByVeterinarioAndDiaAsync(vetId, DayOfWeek.Tuesday)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(new TimeSpan(8, 30, 0), result[0].HoraInicio);
        Assert.Equal(new TimeSpan(14, 0, 0), result[1].HoraInicio);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
