namespace DataAccess.Entities;

public class DisponibilidadVeterinario
{
    public Guid Id { get; set; }
    public Guid VeterinarioId { get; set; }
    public DayOfWeek DiaSemana { get; set; }
    public string Jornada { get; set; } = "Manana"; // Manana, Tarde
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public bool Activo { get; set; } = true;

    // Navegación
    public Usuario Veterinario { get; set; } = null!;
}
