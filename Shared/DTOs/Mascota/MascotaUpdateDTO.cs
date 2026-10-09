namespace Shared.DTOs.Mascota;

public class MascotaUpdateDTO
{
    public string Nombre { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public decimal Peso { get; set; }
    public string? Observaciones { get; set; }
}
