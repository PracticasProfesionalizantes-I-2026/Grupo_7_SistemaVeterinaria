namespace Shared.DTOs.Auth;

public class LoginResponseDTO
{
    public string? Token { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool RequiereCambioPassword { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
