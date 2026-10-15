namespace DataAccess.Entities;

public class EstudioComplementario
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Resultado { get; set; }

    // Navegación
    public AtencionMedica AtencionMedica { get; set; } = null!;
}
