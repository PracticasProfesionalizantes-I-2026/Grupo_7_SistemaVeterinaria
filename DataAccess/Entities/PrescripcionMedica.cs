namespace DataAccess.Entities;

public class PrescripcionMedica
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string Medicamento { get; set; } = string.Empty;
    public string Dosis { get; set; } = string.Empty;
    public string Frecuencia { get; set; } = string.Empty;
    public string Duracion { get; set; } = string.Empty;

    // Navegación
    public AtencionMedica AtencionMedica { get; set; } = null!;
}
