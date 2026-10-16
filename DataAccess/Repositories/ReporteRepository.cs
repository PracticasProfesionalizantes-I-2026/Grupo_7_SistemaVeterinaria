using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class ReporteRepository : IReporteRepository
{
    private readonly VeterinariaDbContext _context;

    public ReporteRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<Reporte> CreateAsync(Reporte reporte)
    {
        reporte.Id = Guid.NewGuid();
        if (reporte.FechaGeneracion == default)
        {
            reporte.FechaGeneracion = DateTime.UtcNow;
        }

        await _context.Reportes.AddAsync(reporte);
        await _context.SaveChangesAsync();
        return reporte;
    }

    public async Task<Reporte?> GetByIdAsync(Guid id)
    {
        return await _context.Reportes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reporte>> GetAllAsync()
    {
        return await _context.Reportes
            .AsNoTracking()
            .OrderByDescending(r => r.FechaGeneracion)
            .ToListAsync();
    }
}
