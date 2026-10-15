using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.AtencionMedica;

public class AtencionMedicaCreateDTO
{
    [Required(ErrorMessage = "El identificador de la mascota es obligatorio.")]
    public Guid MascotaId { get; set; }

    [Required(ErrorMessage = "El motivo de consulta es obligatorio.")]
    [MaxLength(250, ErrorMessage = "El motivo de consulta no puede superar los 250 caracteres.")]
    public string MotivoConsulta { get; set; } = string.Empty;

    public string? Anamnesis { get; set; }

    public string? ExamenFisico { get; set; }

    [Required(ErrorMessage = "El diagnóstico médico es obligatorio.")]
    [MaxLength(1000, ErrorMessage = "El diagnóstico no puede superar los 1000 caracteres.")]
    public string Diagnostico { get; set; } = string.Empty;

    [Required(ErrorMessage = "El tratamiento médico es obligatorio.")]
    [MaxLength(1000, ErrorMessage = "El tratamiento no puede superar los 1000 caracteres.")]
    public string Tratamiento { get; set; } = string.Empty;

    [Required(ErrorMessage = "El peso registrado de la mascota es obligatorio.")]
    [Range(0.01, 500.0, ErrorMessage = "El peso registrado de la mascota es obligatorio y debe ser mayor a cero.")]
    public decimal PesoRegistrado { get; set; }

    [MaxLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    public string? Observaciones { get; set; }

    // Procedimiento quirúrgico opcional (INF-06)
    public ProcedimientoQuirurgicoCreateDTO? ProcedimientoQuirurgico { get; set; }

    // Prescripciones farmacológicas opcionales (RN-02)
    public List<PrescripcionCreateDTO> Prescripciones { get; set; } = new();

    // Estudios complementarios opcionales (RN-02, AMB-01)
    public List<EstudioComplementarioCreateDTO> Estudios { get; set; } = new();

    // Evaluación de aptitud para vacunación in situ (RN-03, RN-04)
    public EvaluacionAptitudVacunacionCreateDTO? EvaluacionAptitud { get; set; }

    // Datos de vacunación aplicada (condicionada a resultado APTO)
    public VacunacionCreateDTO? Vacunacion { get; set; }

    // Seguimiento crónico (RN-11 / CU-06)
    public bool EsEnfermedadCronica { get; set; }
    public SeguimientoCronicoCreateDTO? SeguimientoCronico { get; set; }

    // Compatibilidad retroactiva con contratos preliminares
    public bool? EsAptoVacunacion { get; set; }
    public string? VacunaAplicada { get; set; }
}
