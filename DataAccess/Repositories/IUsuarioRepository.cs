using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IUsuarioRepository
{
    Task<IReadOnlyList<Usuario>> GetAllAsync(string? username = null, string? rol = null, string? estado = null);
    Task<Usuario?> GetByIdAsync(Guid id);
    Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<Usuario?> GetByIdTrackedAsync(Guid id);
    Task<Usuario?> GetByUsernameOrEmailTrackedAsync(string usernameOrEmail);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task DesbloquearConAuditoriaAsync(Usuario usuario, RegistroOperacionUsuario auditoria);
    Task RestablecerPasswordConAuditoriaAsync(Usuario usuario, RegistroOperacionUsuario auditoria);
}
