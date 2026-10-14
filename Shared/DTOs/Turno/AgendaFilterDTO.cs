namespace Shared.DTOs.Turno;

public class AgendaFilterDTO
{
    public DateTime? Fecha { get; set; }
    public Guid? VeterinarioId { get; set; }
    public string? Estado { get; set; }
    public Guid? MascotaId { get; set; }
}
