using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Dueno;

public class DuenoCreateDTO
{
    [Required(ErrorMessage = "El nombre del dueño es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido del dueño es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El DNI del dueño es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El DNI no puede superar los 20 caracteres.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "El DNI debe contener únicamente dígitos numéricos.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El teléfono de contacto es obligatorio.")]
    [MaxLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string Telefono { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email de contacto es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    [EmailAddress(ErrorMessage = "El formato del correo electrónico es inválido.")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string? Direccion { get; set; }
}
