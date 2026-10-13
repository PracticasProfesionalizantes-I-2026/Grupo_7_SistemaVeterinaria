using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IRegistroOperacionUsuarioRepository
{
    Task RegistrarAsync(RegistroOperacionUsuario registro);
    Task<IReadOnlyList<RegistroOperacionUsuario>> GetByUsuarioIdAsync(Guid usuarioId);
}
