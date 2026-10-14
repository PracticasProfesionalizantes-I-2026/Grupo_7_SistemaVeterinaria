using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Mascota;

public class ReasignarDuenoDTO
{
    [Required(ErrorMessage = "El identificador del nuevo dueño es obligatorio.")]
    public Guid NuevoDuenoId { get; set; }
}
