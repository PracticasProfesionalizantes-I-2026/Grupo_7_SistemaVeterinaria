using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Security;

namespace DataAccess.Seeding;

public static class DbInitializer
{
    public static async Task InitializeAsync(VeterinariaDbContext context)
    {
        // Aplica migraciones pendientes o crea la base de datos
        await context.Database.MigrateAsync();

        // Sembrar Usuarios de prueba si la tabla está vacía
        if (!await context.Usuarios.AnyAsync())
        {
            var admin = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "admin",
                Email = "admin@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Admin1234!"),
                Rol = "Administrador",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };

            var veterinario = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "vet.carlos",
                Email = "carlos.veterinario@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Vet1234!"),
                Rol = "Veterinario",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };

            var recepcionista = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "recep.ana",
                Email = "ana.recepcion@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Recep1234!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };

            var bloqueado = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "user.bloqueado",
                Email = "bloqueado@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("User1234!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = true,
                IntentosFallidos = 5,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };

            var inactivo = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "user.inactivo",
                Email = "inactivo@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("User1234!"),
                Rol = "Veterinario",
                Activo = false,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = false,
                FechaCreacion = DateTime.UtcNow
            };

            var temporal = new Usuario
            {
                Id = Guid.NewGuid(),
                Username = "user.temporal",
                Email = "temporal@patitas.com",
                PasswordHash = PasswordHelper.HashPassword("Temporal1234!"),
                Rol = "Recepcionista",
                Activo = true,
                Bloqueado = false,
                IntentosFallidos = 0,
                RequiereCambioPassword = true,
                FechaCreacion = DateTime.UtcNow
            };

            await context.Usuarios.AddRangeAsync(admin, veterinario, recepcionista, bloqueado, inactivo, temporal);
            await context.SaveChangesAsync();
        }

        // Verificar si ya existen datos clínicos y de dueños
        if (await context.Duenos.AnyAsync())
        {
            return; // Ya fue inicializada
        }

        // 1. Sembrar Dueños
        var dueno1 = new Dueno
        {
            Id = Guid.NewGuid(),
            Nombre = "Carlos",
            Apellido = "González",
            Dni = "34567890",
            Telefono = "11-4567-8901",
            Email = "carlos.gonzalez@example.com",
            Direccion = "Av. Corrientes 1234, CABA",
            FechaRegistro = DateTime.UtcNow.AddMonths(-6)
        };

        var dueno2 = new Dueno
        {
            Id = Guid.NewGuid(),
            Nombre = "Mariana",
            Apellido = "López",
            Dni = "38123456",
            Telefono = "11-9876-5432",
            Email = "mariana.lopez@example.com",
            Direccion = "Calle Falsa 742, Lanús",
            FechaRegistro = DateTime.UtcNow.AddMonths(-3)
        };

        await context.Duenos.AddRangeAsync(dueno1, dueno2);
        await context.SaveChangesAsync();

        // 2. Sembrar Mascotas
        var mascota1 = new Mascota
        {
            Id = Guid.NewGuid(),
            DuenoId = dueno1.Id,
            Nombre = "Milo",
            Especie = "Perro",
            Raza = "Golden Retriever",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-3),
            Peso = 28.5m,
            Activa = true,
            Observaciones = "Paciente dócil, alérgico a ciertas marcas de antipulgas.",
            FechaUltimaVisita = DateTime.UtcNow.AddDays(-15)
        };

        var mascota2 = new Mascota
        {
            Id = Guid.NewGuid(),
            DuenoId = dueno1.Id,
            Nombre = "Luna",
            Especie = "Gato",
            Raza = "Siamés",
            Sexo = "Hembra",
            FechaNacimiento = DateTime.UtcNow.AddYears(-2),
            Peso = 4.2m,
            Activa = true,
            Observaciones = "Vacunación al día, esterilizada.",
            FechaUltimaVisita = null
        };

        var mascota3 = new Mascota
        {
            Id = Guid.NewGuid(),
            DuenoId = dueno2.Id,
            Nombre = "Rocky",
            Especie = "Perro",
            Raza = "Boxer",
            Sexo = "Macho",
            FechaNacimiento = DateTime.UtcNow.AddYears(-5),
            Peso = 32.0m,
            Activa = false, // Mascota Inactiva de prueba para validar RN-07 / MEJ-03
            Observaciones = "Baja lógica por mudanza del propietario fuera de la jurisdicción.",
            FechaUltimaVisita = DateTime.UtcNow.AddMonths(-2)
        };

        await context.Mascotas.AddRangeAsync(mascota1, mascota2, mascota3);
        await context.SaveChangesAsync();

        // 3. Sembrar Atenciones Médicas
        var atencion1 = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascota1.Id,
            Fecha = DateTime.UtcNow.AddDays(-15),
            MotivoConsulta = "Control anual y vacunación antirrábica",
            Diagnostico = "Paciente clínicamente sano, parámetros vitales normales.",
            Tratamiento = "Aplicación de vacuna antirrábica dosis anual.",
            PesoRegistrado = 28.5m,
            EsAptoVacunacion = true,
            VacunaAplicada = "Antirrábica Canina",
            Observaciones = "Se indica control en 12 meses."
        };

        var atencion2 = new AtencionMedica
        {
            Id = Guid.NewGuid(),
            MascotaId = mascota3.Id,
            Fecha = DateTime.UtcNow.AddMonths(-2),
            MotivoConsulta = "Revisión general y desparasitación previa a mudanza",
            Diagnostico = "Estado general bueno, leve gingivitis.",
            Tratamiento = "Limpieza dental recomendada y desparasitario interno oral.",
            PesoRegistrado = 32.0m,
            EsAptoVacunacion = false,
            VacunaAplicada = null,
            Observaciones = "No se aplica vacuna por encontrarse bajo tratamiento desparasitante."
        };

        await context.AtencionesMedicas.AddRangeAsync(atencion1, atencion2);
        await context.SaveChangesAsync();
    }
}
