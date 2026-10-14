namespace Shared.DTOs.Turno;

public class TurnoResponseDTO
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public string MascotaNombre { get; set; } = string.Empty;
    public Guid DuenoId { get; set; }
    public string DuenoNombreCompleto { get; set; } = string.Empty;
    public Guid VeterinarioId { get; set; }
    public string VeterinarioNombreCompleto { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool EsAutomatico { get; set; }
    public string TipoOrigen { get; set; } = string.Empty;
    public Guid? AtencionMedicaId { get; set; }
    public string? MotivoCancelacion { get; set; }
    public DateTime? FechaCancelacion { get; set; }
    public string? UsuarioCancelacionUsername { get; set; }
    public DateTime FechaCreacion { get; set; }
}
