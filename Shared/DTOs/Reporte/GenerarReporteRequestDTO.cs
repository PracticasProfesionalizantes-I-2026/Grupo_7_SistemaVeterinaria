using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.Reporte;

public class GenerarReporteRequestDTO
{
    [Required(ErrorMessage = "El motivo del reporte es obligatorio.")]
    [MaxLength(200, ErrorMessage = "El motivo no puede superar los 200 caracteres.")]
    public string Motivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe especificar al menos un tipo de reporte.")]
    [MinLength(1, ErrorMessage = "Debe especificar al menos un tipo de reporte.")]
    public List<string> TiposReporte { get; set; } = new();

    [Required(ErrorMessage = "La fecha desde es obligatoria.")]
    public DateTime FechaDesde { get; set; }

    [Required(ErrorMessage = "La fecha hasta es obligatoria.")]
    public DateTime FechaHasta { get; set; }
}
