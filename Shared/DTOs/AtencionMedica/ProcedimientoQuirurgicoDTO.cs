using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class ProcedimientoQuirurgicoCreateDTO
{
    [Required(ErrorMessage = "El tipo de procedimiento quirúrgico es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El tipo de procedimiento no puede superar los 100 caracteres.")]
    public string TipoProcedimiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción de la intervención quirúrgica es obligatoria.")]
    [MaxLength(1000, ErrorMessage = "La descripción no puede superar los 1000 caracteres.")]
    public string Descripcion { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Las observaciones o complicaciones no pueden superar los 500 caracteres.")]
    public string? ObservacionesComplicaciones { get; set; }
}

public class ProcedimientoQuirurgicoResponseDTO
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string TipoProcedimiento { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public Guid VeterinarioId { get; set; }
    public string? VeterinarioNombre { get; set; }
    public string? ObservacionesComplicaciones { get; set; }
}
