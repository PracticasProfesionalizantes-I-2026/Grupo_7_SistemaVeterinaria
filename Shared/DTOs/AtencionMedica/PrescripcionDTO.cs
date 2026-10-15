using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class PrescripcionCreateDTO
{
    [Required(ErrorMessage = "El medicamento es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El medicamento no puede superar los 150 caracteres.")]
    public string Medicamento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La dosis es obligatoria.")]
    [MaxLength(100, ErrorMessage = "La dosis no puede superar los 100 caracteres.")]
    public string Dosis { get; set; } = string.Empty;

    [Required(ErrorMessage = "La frecuencia es obligatoria.")]
    [MaxLength(100, ErrorMessage = "La frecuencia no puede superar los 100 caracteres.")]
    public string Frecuencia { get; set; } = string.Empty;

    [Required(ErrorMessage = "La duración del tratamiento es obligatoria.")]
    [MaxLength(100, ErrorMessage = "La duración no puede superar los 100 caracteres.")]
    public string Duracion { get; set; } = string.Empty;
}

public class PrescripcionResponseDTO
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;
}
