namespace Shared.DTOs.Mascota;

public class MascotaResponseDTO
{
    public Guid Id { get; set; }
    public Guid DuenoId { get; set; }
    public string NombreDueno { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public decimal Peso { get; set; }
    public bool Activa { get; set; }
    public DateTime? FechaUltimaVisita { get; set; }
    public string? Observaciones { get; set; }
}
