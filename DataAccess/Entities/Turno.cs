namespace DataAccess.Entities;

public class Turno
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public Guid VeterinarioId { get; set; }
    
    // Identificador opcional para vinculación futura con CU-04 (desacoplado sin FK estricta a nivel base)
    public Guid? AtencionMedicaId { get; set; }

    public DateTime FechaHora { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Estado { get; set; } = "Confirmado"; // Pendiente, Confirmado, EnAtencion, Finalizado, Cancelado
    public bool EsAutomatico { get; set; } = false;
    public string TipoOrigen { get; set; } = "Manual"; // Manual, SeguimientoCronico

    // Metadatos de cancelación (INF-05)
    public string? MotivoCancelacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public Guid? UsuarioCancelacionId { get; set; }
    public string? UsuarioCancelacionUsername { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // Navegación (unidireccional o referencial)
    public Mascota Mascota { get; set; } = null!;
    public Usuario Veterinario { get; set; } = null!;
}
