namespace Shared.DTOs.Usuario;

public class RestablecerPasswordResponseDTO
{
    public Guid UsuarioId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordTemporal { get; set; } = string.Empty;
    public bool RequiereCambioPassword { get; set; }
    public bool Bloqueado { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public string Advertencia { get; set; } = string.Empty;
}
