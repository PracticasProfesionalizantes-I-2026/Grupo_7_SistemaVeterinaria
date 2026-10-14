using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class ConfigurarDisponibilidadDTO
{
    [Required(ErrorMessage = "El ID del veterinario es obligatorio.")]
    public Guid VeterinarioId { get; set; }

    [Required(ErrorMessage = "La lista de disponibilidades es obligatoria.")]
    public List<DisponibilidadVeterinarioDTO> Disponibilidades { get; set; } = new();
}
