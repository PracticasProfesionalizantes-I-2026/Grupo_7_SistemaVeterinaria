using Shared.DTOs.Usuario;

namespace BusinessLogic.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync(string? username = null, string? rol = null, string? estado = null);
    Task<UsuarioResponseDTO> GetByIdAsync(Guid id);
}
