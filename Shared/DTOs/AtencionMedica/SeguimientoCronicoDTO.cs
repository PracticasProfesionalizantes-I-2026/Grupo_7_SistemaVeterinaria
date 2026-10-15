using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class SeguimientoCronicoCreateDTO
{
    [Required(ErrorMessage = "La frecuencia de seguimiento es obligatoria.")]
    public string Frecuencia { get; set; } = string.Empty; // "Quincenal", "Mensual", "Bimestral", "Trimestral"

    [Required(ErrorMessage = "La jornada preferida es obligatoria.")]
    public string JornadaPreferida { get; set; } = string.Empty; // "Manana", "Tarde", "Indistinta"
}
