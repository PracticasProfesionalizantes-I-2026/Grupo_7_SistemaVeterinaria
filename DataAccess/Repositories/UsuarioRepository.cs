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

    public async Task<IReadOnlyList<Usuario>> GetAllAsync(string? username = null, string? rol = null, string? estado = null)
    {
        var query = _context.Usuarios
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(username))
        {
            var normalizedUser = username.Trim().ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(normalizedUser));
        }

        if (!string.IsNullOrWhiteSpace(rol))
        {
            var normalizedRol = rol.Trim().ToLower();
            query = query.Where(u => u.Rol.ToLower() == normalizedRol);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            var normalizedEstado = estado.Trim().ToLower();
            if (normalizedEstado == "inactivo")
            {
                query = query.Where(u => !u.Activo);
            }
            else if (normalizedEstado == "bloqueado")
            {
                query = query.Where(u => u.Activo && u.Bloqueado);
            }
            else if (normalizedEstado == "activo")
            {
                query = query.Where(u => u.Activo && !u.Bloqueado);
            }
        }

        return await query
            .OrderBy(u => u.Username)
            .ToListAsync();
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

    public async Task DesbloquearConAuditoriaAsync(Usuario usuario, RegistroOperacionUsuario auditoria)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Usuarios.Update(usuario);
            await _context.RegistrosOperacionesUsuarios.AddAsync(auditoria);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RestablecerPasswordConAuditoriaAsync(Usuario usuario, RegistroOperacionUsuario auditoria)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Usuarios.Update(usuario);
            await _context.RegistrosOperacionesUsuarios.AddAsync(auditoria);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
