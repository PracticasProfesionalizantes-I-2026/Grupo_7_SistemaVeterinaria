using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class HistorialPropietarioRepository : IHistorialPropietarioRepository
{
    private readonly VeterinariaDbContext _context;

    public HistorialPropietarioRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<HistorialPropietario> CreateAsync(HistorialPropietario historial)
    {
        historial.Id = Guid.NewGuid();
        historial.FechaReasignacion = DateTime.UtcNow;
        await _context.HistorialesPropietarios.AddAsync(historial);
        await _context.SaveChangesAsync();
        return historial;
    }

    public async Task<IEnumerable<HistorialPropietario>> GetByMascotaIdAsync(Guid mascotaId)
    {
        return await _context.HistorialesPropietarios
            .AsNoTracking()
            .Include(h => h.DuenoAnterior)
            .Include(h => h.NuevoDueno)
            .Include(h => h.Usuario)
            .Where(h => h.MascotaId == mascotaId)
            .OrderByDescending(h => h.FechaReasignacion)
            .ToListAsync();
    }
}
