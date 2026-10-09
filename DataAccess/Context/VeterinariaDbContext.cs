using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Context;

public class VeterinariaDbContext : DbContext
{
    public VeterinariaDbContext(DbContextOptions<VeterinariaDbContext> options) : base(options)
    {
    }

    public DbSet<Dueno> Duenos => Set<Dueno>();
    public DbSet<Mascota> Mascotas => Set<Mascota>();
    public DbSet<AtencionMedica> AtencionesMedicas => Set<AtencionMedica>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Dueno
        modelBuilder.Entity<Dueno>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.HasIndex(d => d.Dni).IsUnique();
            entity.Property(d => d.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Apellido).IsRequired().HasMaxLength(100);
            entity.Property(d => d.Dni).IsRequired().HasMaxLength(20);
            entity.Property(d => d.Telefono).IsRequired().HasMaxLength(30);
            entity.Property(d => d.Email).IsRequired().HasMaxLength(150);
            entity.Property(d => d.Direccion).HasMaxLength(200);
            entity.Property(d => d.FechaRegistro).IsRequired();
        });

        // Configuración de Mascota
        modelBuilder.Entity<Mascota>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Nombre).IsRequired().HasMaxLength(100);
            entity.Property(m => m.Especie).IsRequired().HasMaxLength(50);
            entity.Property(m => m.Raza).IsRequired().HasMaxLength(50);
            entity.Property(m => m.Sexo).IsRequired().HasMaxLength(20);
            entity.Property(m => m.FechaNacimiento).IsRequired();
            entity.Property(m => m.Peso).HasPrecision(6, 2);
            entity.Property(m => m.Activa).IsRequired().HasDefaultValue(true);
            entity.Property(m => m.Observaciones).HasMaxLength(500);

            // Relación hija 1:N con DeleteBehavior.Restrict
            entity.HasOne(m => m.Dueno)
                  .WithMany(d => d.Mascotas)
                  .HasForeignKey(m => m.DuenoId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de AtencionMedica
        modelBuilder.Entity<AtencionMedica>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Fecha).IsRequired();
            entity.Property(a => a.MotivoConsulta).IsRequired().HasMaxLength(250);
            entity.Property(a => a.Diagnostico).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.Tratamiento).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.PesoRegistrado).HasPrecision(6, 2);
            entity.Property(a => a.EsAptoVacunacion).IsRequired();
            entity.Property(a => a.VacunaAplicada).HasMaxLength(100);
            entity.Property(a => a.Observaciones).HasMaxLength(500);

            // Relación hija 1:N con DeleteBehavior.Restrict
            entity.HasOne(a => a.Mascota)
                  .WithMany(m => m.AtencionesMedicas)
                  .HasForeignKey(a => a.MascotaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
