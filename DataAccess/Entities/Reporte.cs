namespace DataAccess.Entities;

public class Reporte
{
    public Guid Id { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime FechaDesde { get; set; }
    public DateTime FechaHasta { get; set; }
    public string TiposReporteJson { get; set; } = "[]";
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

    // Usuario Administrador que generó el reporte
    public Guid UsuarioGeneradorId { get; set; }
    public string UsuarioGeneradorUsername { get; set; } = string.Empty;

    // Resumen cuantitativo consolidado
    public int TotalAtenciones { get; set; }
    public int TotalVacunaciones { get; set; }
    public int TotalMascotas { get; set; }
    public int TotalDuenos { get; set; }
    public int TotalTurnos { get; set; }

    // Desglose analítico completo serializado en JSON (inmutable)
    public string DatosConsolidadosJson { get; set; } = "{}";
}
