using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class AtencionMedicaRepository : IAtencionMedicaRepository
{
    private readonly VeterinariaDbContext _context;

    public AtencionMedicaRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AtencionMedica>> GetAllAsync()
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Include(a => a.Mascota)
            .Include(a => a.Veterinario)
            .Include(a => a.ProcedimientoQuirurgico)
            .Include(a => a.Prescripciones)
            .Include(a => a.EstudiosComplementarios)
            .Include(a => a.TurnosSeguimiento)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<AtencionMedica?> GetByIdAsync(Guid id)
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Include(a => a.Mascota)
            .Include(a => a.Veterinario)
            .Include(a => a.ProcedimientoQuirurgico)
            .Include(a => a.Prescripciones)
            .Include(a => a.EstudiosComplementarios)
            .Include(a => a.TurnosSeguimiento)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AtencionMedica>> GetByMascotaIdAsync(Guid mascotaId)
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Include(a => a.Mascota)
            .Include(a => a.Veterinario)
            .Include(a => a.ProcedimientoQuirurgico)
                .ThenInclude(p => p!.Veterinario)
            .Include(a => a.Prescripciones)
            .Include(a => a.EstudiosComplementarios)
            .Include(a => a.TurnosSeguimiento)
            .Where(a => a.MascotaId == mascotaId)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<AtencionMedica?> GetUltimaAtencionByMascotaIdAsync(Guid mascotaId)
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Where(a => a.MascotaId == mascotaId)
            .OrderByDescending(a => a.Fecha)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> HasTurnosSeguimientoAsync(Guid atencionMedicaId)
    {
        return await _context.Turnos
            .AsNoTracking()
            .AnyAsync(t => t.AtencionMedicaId == atencionMedicaId);
    }

    public async Task<AtencionMedica> CreateAsync(AtencionMedica atencionMedica)
    {
        if (atencionMedica.Id == Guid.Empty)
        {
            atencionMedica.Id = Guid.NewGuid();
        }

        if (atencionMedica.Fecha == default)
        {
            atencionMedica.Fecha = DateTime.UtcNow;
        }

        // Si incluye procedimiento quirúrgico, asegurar sus IDs y fechas
        if (atencionMedica.ProcedimientoQuirurgico != null)
        {
            if (atencionMedica.ProcedimientoQuirurgico.Id == Guid.Empty)
            {
                atencionMedica.ProcedimientoQuirurgico.Id = Guid.NewGuid();
            }
            atencionMedica.ProcedimientoQuirurgico.AtencionMedicaId = atencionMedica.Id;
            atencionMedica.ProcedimientoQuirurgico.Fecha = atencionMedica.Fecha;
            if (atencionMedica.VeterinarioId.HasValue && atencionMedica.ProcedimientoQuirurgico.VeterinarioId == Guid.Empty)
            {
                atencionMedica.ProcedimientoQuirurgico.VeterinarioId = atencionMedica.VeterinarioId.Value;
            }
        }

        // Asignar IDs a prescripciones
        foreach (var p in atencionMedica.Prescripciones)
        {
            if (p.Id == Guid.Empty)
            {
                p.Id = Guid.NewGuid();
            }
            p.AtencionMedicaId = atencionMedica.Id;
        }

        // Asignar IDs a estudios
        foreach (var e in atencionMedica.EstudiosComplementarios)
        {
            if (e.Id == Guid.Empty)
            {
                e.Id = Guid.NewGuid();
            }
            e.AtencionMedicaId = atencionMedica.Id;
        }

        // INF-07: Actualización atómica de la última visita de la mascota dentro de la misma transacción:
        var mascota = await _context.Mascotas
            .Include(m => m.Dueno)
            .FirstOrDefaultAsync(m => m.Id == atencionMedica.MascotaId);
        if (mascota != null)
        {
            // Regla estricta de no retroceso: actualizar solo si no tenía visitas o si la fecha actual es más reciente
            if (!mascota.FechaUltimaVisita.HasValue || atencionMedica.Fecha > mascota.FechaUltimaVisita.Value)
            {
                mascota.FechaUltimaVisita = atencionMedica.Fecha;
            }
            atencionMedica.Mascota = mascota;
        }

        await _context.AtencionesMedicas.AddAsync(atencionMedica);
        await _context.SaveChangesAsync();
        return atencionMedica;
    }
}
