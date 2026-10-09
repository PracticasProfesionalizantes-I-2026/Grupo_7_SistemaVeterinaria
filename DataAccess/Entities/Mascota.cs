namespace DataAccess.Entities;

public class Mascota
{
    public Guid Id { get; set; }
    public Guid DuenoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public decimal Peso { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime? FechaUltimaVisita { get; set; }
    public string? Observaciones { get; set; }

    // Navegación
    public Dueno Dueno { get; set; } = null!;
    public ICollection<AtencionMedica> AtencionesMedicas { get; set; } = new List<AtencionMedica>();
}
