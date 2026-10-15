using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class EvaluacionAptitudVacunacionCreateDTO
{
    public bool SinFiebre { get; set; }
    public bool BuenEstadoGeneral { get; set; }
    public bool SinEnfermedadInfecciosaAguda { get; set; }
    public bool AutorizacionVeterinaria { get; set; }

    [MaxLength(500, ErrorMessage = "El motivo de no aptitud no puede superar los 500 caracteres.")]
    public string? MotivoNoApto { get; set; }

    public bool EsApto => SinFiebre && BuenEstadoGeneral && SinEnfermedadInfecciosaAguda && AutorizacionVeterinaria;
    public string Resultado => EsApto ? "APTO" : "NO_APTO";
}

public class EvaluacionAptitudVacunacionResponseDTO
{
    public bool SinFiebre { get; set; }
    public bool BuenEstadoGeneral { get; set; }
    public bool SinEnfermedadInfecciosaAguda { get; set; }
    public bool AutorizacionVeterinaria { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? MotivoNoApto { get; set; }
}
