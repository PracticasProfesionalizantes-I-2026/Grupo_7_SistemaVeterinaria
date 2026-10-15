using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class EstudioComplementarioCreateDTO
{
    [Required(ErrorMessage = "El nombre del estudio complementario es obligatorio.")]
    [MaxLength(150, ErrorMessage = "El nombre del estudio no puede superar los 150 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [MaxLength(1000, ErrorMessage = "El resultado no puede superar los 1000 caracteres.")]
    public string? Resultado { get; set; }
}

public class EstudioComplementarioResponseDTO
{
    public Guid Id { get; set; }
    public Guid AtencionMedicaId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Resultado { get; set; }
}
