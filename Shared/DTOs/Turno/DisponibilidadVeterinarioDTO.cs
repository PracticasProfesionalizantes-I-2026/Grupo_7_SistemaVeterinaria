using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Turno;

public class DisponibilidadVeterinarioDTO
{
    [Required(ErrorMessage = "El día de la semana es obligatorio.")]
    public DayOfWeek DiaSemana { get; set; }

    [Required(ErrorMessage = "La jornada es obligatoria (Manana o Tarde).")]
    public string Jornada { get; set; } = string.Empty; // "Manana", "Tarde"

    [Required(ErrorMessage = "La hora de inicio es obligatoria.")]
    public TimeSpan HoraInicio { get; set; }

    [Required(ErrorMessage = "La hora de fin es obligatoria.")]
    public TimeSpan HoraFin { get; set; }
}
