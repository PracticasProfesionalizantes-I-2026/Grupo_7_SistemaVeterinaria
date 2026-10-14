using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class TurnoUpdateDTO
{
    [Required(ErrorMessage = "La nueva fecha y hora del turno son obligatorias.")]
    public DateTime FechaHora { get; set; }

    public Guid? VeterinarioId { get; set; }

    [StringLength(250, ErrorMessage = "El motivo de consulta no puede superar los 250 caracteres.")]
    public string? MotivoConsulta { get; set; }
}
