using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class TurnoCreateDTO
{
    [Required(ErrorMessage = "El ID de la mascota es obligatorio.")]
    public Guid MascotaId { get; set; }

    [Required(ErrorMessage = "El ID del veterinario es obligatorio.")]
    public Guid VeterinarioId { get; set; }

    [Required(ErrorMessage = "La fecha y hora del turno son obligatorias.")]
    public DateTime FechaHora { get; set; }

    [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
    [StringLength(250, ErrorMessage = "El motivo de consulta no puede superar los 250 caracteres.")]
    public string MotivoConsulta { get; set; } = string.Empty;
}
