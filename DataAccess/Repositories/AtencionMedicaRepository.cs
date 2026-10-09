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
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<AtencionMedica?> GetByIdAsync(Guid id)
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Include(a => a.Mascota)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<AtencionMedica>> GetByMascotaIdAsync(Guid mascotaId)
    {
        return await _context.AtencionesMedicas
            .AsNoTracking()
            .Include(a => a.Mascota)
            .Where(a => a.MascotaId == mascotaId)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<AtencionMedica> CreateAsync(AtencionMedica atencionMedica)
    {
        atencionMedica.Id = Guid.NewGuid();
        if (atencionMedica.Fecha == default)
        {
            atencionMedica.Fecha = DateTime.UtcNow;
        }

        await _context.AtencionesMedicas.AddAsync(atencionMedica);
        await _context.SaveChangesAsync();
        return atencionMedica;
    }
}
