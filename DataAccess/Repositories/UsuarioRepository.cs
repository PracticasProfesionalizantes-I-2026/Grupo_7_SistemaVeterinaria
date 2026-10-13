using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly VeterinariaDbContext _context;

    public UsuarioRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<Usuario?> GetByIdAsync(Guid id)
    {
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim().ToLower();
        return await _context.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);
    }

    public async Task<Usuario?> GetByIdTrackedAsync(Guid id)
    {
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> GetByUsernameOrEmailTrackedAsync(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim().ToLower();
        return await _context.Usuarios
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized);
    }

    public async Task<Usuario> CreateAsync(Usuario usuario)
    {
        usuario.Id = Guid.NewGuid();
        usuario.FechaCreacion = DateTime.UtcNow;
        await _context.Usuarios.AddAsync(usuario);
        await _context.SaveChangesAsync();
        return usuario;
    }

    public async Task UpdateAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        await _context.SaveChangesAsync();
    }
}
