using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Mascota;

public class MascotaUpdateDTO
{
    [Required(ErrorMessage = "El nombre de la mascota es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La raza de la mascota es obligatoria.")]
    [MaxLength(50, ErrorMessage = "La raza no puede superar los 50 caracteres.")]
    public string Raza { get; set; } = string.Empty;

    [Required(ErrorMessage = "El sexo de la mascota es obligatorio.")]
    [MaxLength(20, ErrorMessage = "El sexo no puede superar los 20 caracteres.")]
    public string Sexo { get; set; } = string.Empty;

    [Range(0.01, 1000.00, ErrorMessage = "El peso de la mascota debe ser mayor a 0 kg.")]
    public decimal Peso { get; set; }

    [MaxLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    public string? Observaciones { get; set; }
}
