using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Auth;

public class CambiarPasswordTemporalDTO
{
    [Required(ErrorMessage = "El nombre de usuario o correo electrónico es obligatorio.")]
    public string UsuarioOEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña actual es obligatoria.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
    public string NuevaPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "La confirmación de la nueva contraseña es obligatoria.")]
    public string ConfirmacionPassword { get; set; } = string.Empty;
}
