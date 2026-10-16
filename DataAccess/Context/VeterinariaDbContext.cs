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
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<TokenRevocado> TokensRevocados => Set<TokenRevocado>();
    public DbSet<RegistroOperacionUsuario> RegistrosOperacionesUsuarios => Set<RegistroOperacionUsuario>();
    public DbSet<HistorialPropietario> HistorialesPropietarios => Set<HistorialPropietario>();
    public DbSet<Turno> Turnos => Set<Turno>();
    public DbSet<DisponibilidadVeterinario> DisponibilidadesVeterinarios => Set<DisponibilidadVeterinario>();
    public DbSet<ProcedimientoQuirurgico> ProcedimientosQuirurgicos => Set<ProcedimientoQuirurgico>();
    public DbSet<PrescripcionMedica> PrescripcionesMedicas => Set<PrescripcionMedica>();
    public DbSet<EstudioComplementario> EstudiosComplementarios => Set<EstudioComplementario>();
    public DbSet<Reporte> Reportes => Set<Reporte>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de RegistroOperacionUsuario
        modelBuilder.Entity<RegistroOperacionUsuario>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.UsuarioId).IsRequired();
            entity.Property(r => r.UsernameAfectado).IsRequired().HasMaxLength(50);
            entity.Property(r => r.TipoOperacion).IsRequired().HasMaxLength(50);
            entity.Property(r => r.AdministradorId).IsRequired();
            entity.Property(r => r.AdministradorUsername).IsRequired().HasMaxLength(50);
            entity.Property(r => r.FechaOperacion).IsRequired();
            entity.Property(r => r.Detalles).HasMaxLength(250);
        });

        // Configuración de TokenRevocado
        modelBuilder.Entity<TokenRevocado>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.HasIndex(t => t.Jti).IsUnique();
            entity.Property(t => t.Jti).IsRequired().HasMaxLength(100);
            entity.Property(t => t.UsuarioId).IsRequired();
            entity.Property(t => t.FechaRevocacion).IsRequired();
            entity.Property(t => t.FechaExpiracion).IsRequired();
        });

        // Configuración de Usuario
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Email).IsRequired().HasMaxLength(150);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(255);
            entity.Property(u => u.Rol).IsRequired().HasMaxLength(50);
            entity.Property(u => u.Activo).IsRequired().HasDefaultValue(true);
            entity.Property(u => u.Bloqueado).IsRequired().HasDefaultValue(false);
            entity.Property(u => u.IntentosFallidos).IsRequired().HasDefaultValue(0);
            entity.Property(u => u.RequiereCambioPassword).IsRequired().HasDefaultValue(false);
            entity.Property(u => u.FechaCreacion).IsRequired();
        });

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
            entity.Property(a => a.Anamnesis).IsRequired().HasMaxLength(2000).HasDefaultValue("");
            entity.Property(a => a.ExamenFisico).IsRequired().HasMaxLength(2000).HasDefaultValue("");
            entity.Property(a => a.Diagnostico).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.Tratamiento).IsRequired().HasMaxLength(1000);
            entity.Property(a => a.PesoRegistrado).HasPrecision(6, 2);
            entity.Property(a => a.EsAptoVacunacion).IsRequired();
            entity.Property(a => a.AptitudVacunacionResultado).HasMaxLength(20);
            entity.Property(a => a.MotivoNoAptoVacunacion).HasMaxLength(500);
            entity.Property(a => a.VacunaAplicada).HasMaxLength(100);
            entity.Property(a => a.VacunaDosis).HasMaxLength(50);
            entity.Property(a => a.VacunaLote).HasMaxLength(50);
            entity.Property(a => a.Observaciones).HasMaxLength(500);
            entity.Property(a => a.AlertaDesnutricion).IsRequired().HasDefaultValue(false);
            entity.Property(a => a.PorcentajeDisminucionPeso).HasPrecision(6, 2);
            entity.Property(a => a.PesoReferenciaUtilizado).HasPrecision(6, 2);

            // Relación hija 1:N con DeleteBehavior.Restrict
            entity.HasOne(a => a.Mascota)
                  .WithMany(m => m.AtencionesMedicas)
                  .HasForeignKey(a => a.MascotaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(a => a.Veterinario)
                  .WithMany()
                  .HasForeignKey(a => a.VeterinarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de ProcedimientoQuirurgico
        modelBuilder.Entity<ProcedimientoQuirurgico>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.TipoProcedimiento).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Descripcion).IsRequired().HasMaxLength(1000);
            entity.Property(p => p.Fecha).IsRequired();
            entity.Property(p => p.ObservacionesComplicaciones).HasMaxLength(500);

            entity.HasOne(p => p.AtencionMedica)
                  .WithOne(a => a.ProcedimientoQuirurgico)
                  .HasForeignKey<ProcedimientoQuirurgico>(p => p.AtencionMedicaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Veterinario)
                  .WithMany()
                  .HasForeignKey(p => p.VeterinarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de PrescripcionMedica
        modelBuilder.Entity<PrescripcionMedica>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Medicamento).IsRequired().HasMaxLength(150);
            entity.Property(p => p.Dosis).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Frecuencia).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Duracion).IsRequired().HasMaxLength(100);

            entity.HasOne(p => p.AtencionMedica)
                  .WithMany(a => a.Prescripciones)
                  .HasForeignKey(p => p.AtencionMedicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de EstudioComplementario
        modelBuilder.Entity<EstudioComplementario>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Nombre).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Resultado).HasMaxLength(1000);

            entity.HasOne(e => e.AtencionMedica)
                  .WithMany(a => a.EstudiosComplementarios)
                  .HasForeignKey(e => e.AtencionMedicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de HistorialPropietario
        modelBuilder.Entity<HistorialPropietario>(entity =>
        {
            entity.HasKey(h => h.Id);
            entity.Property(h => h.MascotaId).IsRequired();
            entity.Property(h => h.DuenoAnteriorId).IsRequired();
            entity.Property(h => h.NuevoDuenoId).IsRequired();
            entity.Property(h => h.FechaReasignacion).IsRequired();
            entity.Property(h => h.UsuarioId).IsRequired();
            entity.Property(h => h.UsuarioUsername).IsRequired().HasMaxLength(50);

            entity.HasOne(h => h.Mascota)
                  .WithMany(m => m.HistorialesPropietarios)
                  .HasForeignKey(h => h.MascotaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.DuenoAnterior)
                  .WithMany()
                  .HasForeignKey(h => h.DuenoAnteriorId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.NuevoDueno)
                  .WithMany()
                  .HasForeignKey(h => h.NuevoDuenoId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(h => h.Usuario)
                  .WithMany()
                  .HasForeignKey(h => h.UsuarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Turno
        modelBuilder.Entity<Turno>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.FechaHora).IsRequired();
            entity.Property(t => t.MotivoConsulta).IsRequired().HasMaxLength(250);
            entity.Property(t => t.Estado).IsRequired().HasMaxLength(50);
            entity.Property(t => t.EsAutomatico).IsRequired();
            entity.Property(t => t.TipoOrigen).IsRequired().HasMaxLength(50);
            entity.Property(t => t.MotivoCancelacion).HasMaxLength(250);
            entity.Property(t => t.UsuarioCancelacionUsername).HasMaxLength(50);
            entity.Property(t => t.FechaCreacion).IsRequired();

            entity.HasIndex(t => new { t.VeterinarioId, t.FechaHora });
            entity.HasIndex(t => t.AtencionMedicaId);

            entity.HasOne(t => t.Mascota)
                  .WithMany()
                  .HasForeignKey(t => t.MascotaId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Veterinario)
                  .WithMany()
                  .HasForeignKey(t => t.VeterinarioId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.AtencionMedica)
                  .WithMany(a => a.TurnosSeguimiento)
                  .HasForeignKey(t => t.AtencionMedicaId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de DisponibilidadVeterinario
        modelBuilder.Entity<DisponibilidadVeterinario>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.VeterinarioId).IsRequired();
            entity.Property(d => d.DiaSemana).IsRequired();
            entity.Property(d => d.Jornada).IsRequired().HasMaxLength(20);
            entity.Property(d => d.HoraInicio).IsRequired();
            entity.Property(d => d.HoraFin).IsRequired();
            entity.Property(d => d.Activo).IsRequired().HasDefaultValue(true);

            entity.HasIndex(d => new { d.VeterinarioId, d.DiaSemana, d.Jornada });

            entity.HasOne(d => d.Veterinario)
                  .WithMany()
                  .HasForeignKey(d => d.VeterinarioId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de Reporte (CU-10 y CU-11)
        modelBuilder.Entity<Reporte>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Motivo).IsRequired().HasMaxLength(200);
            entity.Property(r => r.FechaDesde).IsRequired();
            entity.Property(r => r.FechaHasta).IsRequired();
            entity.Property(r => r.TiposReporteJson).IsRequired();
            entity.Property(r => r.FechaGeneracion).IsRequired();
            entity.Property(r => r.UsuarioGeneradorId).IsRequired();
            entity.Property(r => r.UsuarioGeneradorUsername).IsRequired().HasMaxLength(50);
            entity.Property(r => r.DatosConsolidadosJson).IsRequired();

            entity.HasIndex(r => r.FechaGeneracion);
        });
    }
}
