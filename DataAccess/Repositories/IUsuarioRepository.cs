using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IUsuarioRepository
{
    Task<Usuario?> GetByIdAsync(Guid id);
    Task<Usuario?> GetByUsernameOrEmailAsync(string usernameOrEmail);
    Task<Usuario?> GetByIdTrackedAsync(Guid id);
    Task<Usuario?> GetByUsernameOrEmailTrackedAsync(string usernameOrEmail);
    Task<Usuario> CreateAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
}
