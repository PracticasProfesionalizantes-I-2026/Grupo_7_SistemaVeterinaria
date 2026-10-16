namespace Shared.DTOs.Reporte;

public class ReporteResumenDTO
{
    public Guid Id { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public List<string> TiposReporte { get; set; } = new();
    public DateTime FechaGeneracion { get; set; }
    public Guid UsuarioGeneradorId { get; set; }
    public string UsuarioGeneradorUsername { get; set; } = string.Empty;

    public int TotalAtenciones { get; set; }
    public int TotalVacunaciones { get; set; }
    public int TotalMascotas { get; set; }
    public int TotalDuenos { get; set; }
    public int TotalTurnos { get; set; }
}
