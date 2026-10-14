using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class TurnoCancelarDTO
{
    [Required(ErrorMessage = "Debe ingresar un motivo de cancelación")]
    [StringLength(250, ErrorMessage = "El motivo de cancelación no puede superar los 250 caracteres.")]
    public string MotivoCancelacion { get; set; } = string.Empty;
}
