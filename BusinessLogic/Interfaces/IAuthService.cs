using Shared.DTOs.Auth;

namespace BusinessLogic.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto);
    Task<LoginResponseDTO> CambiarPasswordTemporalAsync(CambiarPasswordTemporalDTO dto);
    Task LogoutAsync(string jti, Guid usuarioId, DateTime? fechaExpiracion = null);
}
