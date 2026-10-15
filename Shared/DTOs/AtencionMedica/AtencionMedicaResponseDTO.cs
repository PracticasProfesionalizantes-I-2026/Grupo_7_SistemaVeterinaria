using Shared.DTOs.Turno;

namespace Shared.DTOs.AtencionMedica;

public class AtencionMedicaResponseDTO
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public string NombreMascota { get; set; } = string.Empty;
    public Guid VeterinarioId { get; set; }
    public string NombreVeterinario { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Anamnesis { get; set; } = string.Empty;
    public string ExamenFisico { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public decimal PesoRegistrado { get; set; }
    public string? Observaciones { get; set; }

    // Alerta de desnutrición (Nivel 1 / RN-12)
    public bool AlertaDesnutricion { get; set; }
    public decimal? PorcentajeDisminucionPeso { get; set; }
    public decimal? PesoReferenciaUtilizado { get; set; }
    public string? MensajeAlertaDesnutricion { get; set; }

    // Evaluación de aptitud y vacunación (RN-03, RN-04)
    public bool EsAptoVacunacion { get; set; }
    public string? AptitudVacunacionResultado { get; set; }
    public string? MotivoNoAptoVacunacion { get; set; }
    public EvaluacionAptitudVacunacionResponseDTO? EvaluacionAptitud { get; set; }
    public VacunacionResponseDTO? Vacunacion { get; set; }
    public string? VacunaAplicada { get; set; }

    // Procedimiento quirúrgico (INF-06)
    public ProcedimientoQuirurgicoResponseDTO? ProcedimientoQuirurgico { get; set; }

    // Prescripciones farmacológicas (RN-02)
    public List<PrescripcionResponseDTO> Prescripciones { get; set; } = new();

    // Estudios complementarios (RN-02, AMB-01)
    public List<EstudioComplementarioResponseDTO> Estudios { get; set; } = new();

    // Seguimiento de enfermedades crónicas (RN-11 / CU-06)
    public List<TurnoResponseDTO> TurnosSeguimiento { get; set; } = new();
    public List<DateTime> FechasPendientesManual { get; set; } = new();
    public string? MensajeSeguimientoCronico { get; set; }

    // Confirmación de última visita actualizada (INF-07)
    public DateTime? FechaUltimaVisitaMascota { get; set; }
}
