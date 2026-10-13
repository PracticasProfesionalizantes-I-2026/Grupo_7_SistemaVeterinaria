using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class RegistroOperacionUsuarioRepository : IRegistroOperacionUsuarioRepository
{
    private readonly VeterinariaDbContext _context;

    public RegistroOperacionUsuarioRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(RegistroOperacionUsuario registro)
    {
        registro.Id = Guid.NewGuid();
        registro.FechaOperacion = DateTime.UtcNow;
        await _context.RegistrosOperacionesUsuarios.AddAsync(registro);
        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<RegistroOperacionUsuario>> GetByUsuarioIdAsync(Guid usuarioId)
    {
        return await _context.RegistrosOperacionesUsuarios
            .AsNoTracking()
            .Where(r => r.UsuarioId == usuarioId)
            .OrderByDescending(r => r.FechaOperacion)
            .ToListAsync();
    }
}
