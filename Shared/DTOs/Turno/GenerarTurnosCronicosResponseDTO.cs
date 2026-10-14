namespace Shared.DTOs.Turno;

public class GenerarTurnosCronicosResponseDTO
{
    public List<TurnoResponseDTO> TurnosGenerados { get; set; } = new();
    public List<DateTime> FechasPendientes { get; set; } = new();
    public int TotalGenerados => TurnosGenerados.Count;
    public int TotalPendientes => FechasPendientes.Count;
    public string Mensaje { get; set; } = string.Empty;
}
