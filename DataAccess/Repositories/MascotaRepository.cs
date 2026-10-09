using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class MascotaRepository : IMascotaRepository
{
    private readonly VeterinariaDbContext _context;

    public MascotaRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Mascota>> GetAllAsync(bool? soloActivas = null)
    {
        var query = _context.Mascotas
            .AsNoTracking()
            .Include(m => m.Dueno)
            .AsQueryable();

        if (soloActivas.HasValue)
        {
            query = query.Where(m => m.Activa == soloActivas.Value);
        }

        return await query
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }

    public async Task<Mascota?> GetByIdAsync(Guid id)
    {
        return await _context.Mascotas
            .AsNoTracking()
            .Include(m => m.Dueno)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Mascota?> GetByIdTrackedAsync(Guid id)
    {
        return await _context.Mascotas
            .Include(m => m.Dueno)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<IEnumerable<Mascota>> GetByDuenoIdAsync(Guid duenoId)
    {
        return await _context.Mascotas
            .AsNoTracking()
            .Include(m => m.Dueno)
            .Where(m => m.DuenoId == duenoId)
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }

    public async Task<Mascota> CreateAsync(Mascota mascota)
    {
        mascota.Id = Guid.NewGuid();
        mascota.Activa = true;
        await _context.Mascotas.AddAsync(mascota);
        await _context.SaveChangesAsync();
        return mascota;
    }

    public async Task<Mascota> UpdateAsync(Mascota mascota)
    {
        _context.Mascotas.Update(mascota);
        await _context.SaveChangesAsync();
        return mascota;
    }

    public async Task<bool> UpdateUltimaVisitaAsync(Guid mascotaId, DateTime fechaVisita)
    {
        var mascota = await _context.Mascotas.FindAsync(mascotaId);
        if (mascota == null)
            return false;

        mascota.FechaUltimaVisita = fechaVisita;
        await _context.SaveChangesAsync();
        return true;
    }
}
