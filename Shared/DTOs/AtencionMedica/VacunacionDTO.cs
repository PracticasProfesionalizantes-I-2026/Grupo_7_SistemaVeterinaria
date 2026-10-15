using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class VacunacionCreateDTO
{
    [Required(ErrorMessage = "El nombre de la vacuna es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre de la vacuna no puede superar los 100 caracteres.")]
    public string Vacuna { get; set; } = string.Empty;

    [MaxLength(50, ErrorMessage = "La dosis no puede superar los 50 caracteres.")]
    public string? Dosis { get; set; }

    [MaxLength(50, ErrorMessage = "El lote no puede superar los 50 caracteres.")]
    public string? Lote { get; set; }

    public DateTime? FechaProximaDosis { get; set; }
}

public class VacunacionResponseDTO
{
    public string Vacuna { get; set; } = string.Empty;
    public string? Dosis { get; set; }
    public string? Lote { get; set; }
    public DateTime? FechaProximaDosis { get; set; }
}
