namespace Shared.DTOs.Mascota;

public class MascotaCreateDTO
{
    public Guid DuenoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public decimal Peso { get; set; }
    public string? Observaciones { get; set; }
}
