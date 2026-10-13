using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Auth;

public class LoginRequestDTO
{
    [Required(ErrorMessage = "El nombre de usuario o correo electrónico es obligatorio.")]
    public string UsuarioOEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public string Password { get; set; } = string.Empty;
}
