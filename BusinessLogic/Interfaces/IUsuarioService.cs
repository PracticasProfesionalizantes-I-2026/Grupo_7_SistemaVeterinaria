using Shared.DTOs.Usuario;

namespace BusinessLogic.Interfaces;

public interface IUsuarioService
{
    Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync(string? username = null, string? rol = null, string? estado = null);
    Task<UsuarioResponseDTO> GetByIdAsync(Guid id);
    Task<UsuarioResponseDTO> DesbloquearCuentaAsync(Guid id, Guid administradorId, string administradorUsername);
    Task<RestablecerPasswordResponseDTO> RestablecerPasswordAsync(Guid id, Guid administradorId, string administradorUsername);
}
