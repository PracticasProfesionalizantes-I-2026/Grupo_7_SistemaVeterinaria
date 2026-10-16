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

    public async Task ReasignarDuenoConHistorialAsync(Mascota mascota, HistorialPropietario historial)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Mascotas.Update(mascota);
            await _context.HistorialesPropietarios.AddAsync(historial);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<Mascota>> BuscarAsync(string criterio)
    {
        var query = _context.Mascotas
            .AsNoTracking()
            .Include(m => m.Dueno)
            .Include(m => m.AtencionesMedicas)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(criterio))
        {
            var normalizado = criterio.Trim().ToLower();
            query = query.Where(m => m.Nombre.ToLower().Contains(normalizado) || 
                                     (m.Dueno != null && (m.Dueno.Dni.Contains(normalizado) || 
                                                          m.Dueno.Nombre.ToLower().Contains(normalizado) || 
                                                          m.Dueno.Apellido.ToLower().Contains(normalizado))));
        }

        return await query
            .OrderBy(m => m.Nombre)
            .ToListAsync();
    }
}
