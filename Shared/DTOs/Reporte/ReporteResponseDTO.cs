namespace Shared.DTOs.Reporte;

public class ReporteResponseDTO
{
    public Guid Id { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public List<string> TiposReporte { get; set; } = new();
    public DateTime FechaGeneracion { get; set; }
    public Guid UsuarioGeneradorId { get; set; }
    public string UsuarioGeneradorUsername { get; set; } = string.Empty;

    // Resumen global
    public int TotalAtenciones { get; set; }
    public int TotalVacunaciones { get; set; }
    public int TotalMascotas { get; set; }
    public int TotalDuenos { get; set; }
    public int TotalTurnos { get; set; }

    // Métricas analíticas detalladas por módulo
    public ReporteDetalleDTO DatosReporte { get; set; } = new();
}
