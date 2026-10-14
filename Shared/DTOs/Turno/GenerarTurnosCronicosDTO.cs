using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class GenerarTurnosCronicosDTO
{
    [Required(ErrorMessage = "El ID de la mascota es obligatorio.")]
    public Guid MascotaId { get; set; }

    [Required(ErrorMessage = "El ID del veterinario es obligatorio.")]
    public Guid VeterinarioId { get; set; }

    public Guid? AtencionMedicaId { get; set; }

    [Required(ErrorMessage = "La fecha de inicio o atención es obligatoria.")]
    public DateTime FechaAtencion { get; set; }

    [Required(ErrorMessage = "La frecuencia de seguimiento es obligatoria.")]
    public string Frecuencia { get; set; } = string.Empty; // "Quincenal", "Mensual", "Bimestral", "Trimestral"

    [Required(ErrorMessage = "La jornada preferida es obligatoria.")]
    public string JornadaPreferida { get; set; } = string.Empty; // "Manana", "Tarde", "Indistinta"
}
