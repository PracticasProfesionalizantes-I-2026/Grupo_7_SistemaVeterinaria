using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class DuenoRepository : IDuenoRepository
{
    private readonly VeterinariaDbContext _context;

    public DuenoRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Dueno>> GetAllAsync()
    {
        return await _context.Duenos
            .AsNoTracking()
            .Include(d => d.Mascotas)
            .OrderBy(d => d.Apellido)
            .ThenBy(d => d.Nombre)
            .ToListAsync();
    }

    public async Task<Dueno?> GetByIdAsync(Guid id)
    {
        return await _context.Duenos
            .AsNoTracking()
            .Include(d => d.Mascotas)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Dueno?> GetByIdTrackedAsync(Guid id)
    {
        return await _context.Duenos
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Dueno?> GetByDniAsync(string dni)
    {
        return await _context.Duenos
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Dni == dni);
    }

    public async Task<bool> HasMascotasAsync(Guid duenoId)
    {
        return await _context.Mascotas
            .AsNoTracking()
            .AnyAsync(m => m.DuenoId == duenoId);
    }

    public async Task<Dueno> CreateAsync(Dueno dueno)
    {
        dueno.Id = Guid.NewGuid();
        dueno.FechaRegistro = DateTime.UtcNow;
        await _context.Duenos.AddAsync(dueno);
        await _context.SaveChangesAsync();
        return dueno;
    }

    public async Task<Dueno> UpdateAsync(Dueno dueno)
    {
        _context.Duenos.Update(dueno);
        await _context.SaveChangesAsync();
        return dueno;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var dueno = await _context.Duenos.FindAsync(id);
        if (dueno == null)
            return false;

        _context.Duenos.Remove(dueno);
        await _context.SaveChangesAsync();
        return true;
    }
}
