namespace DataAccess.Entities;

public class ProcedimientoQuirurgico
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string TipoProcedimiento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public Guid VeterinarioId { get; set; }
    public string? ObservacionesComplicaciones { get; set; }

    // Navegación
    public AtencionMedica AtencionMedica { get; set; } = null!;
    public Usuario Veterinario { get; set; } = null!;
}
