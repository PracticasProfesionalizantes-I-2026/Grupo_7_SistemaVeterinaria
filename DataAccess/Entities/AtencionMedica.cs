namespace DataAccess.Entities;

public class AtencionMedica
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public Guid? VeterinarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Anamnesis { get; set; } = string.Empty;
    public string ExamenFisico { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public decimal PesoRegistrado { get; set; }
    public string? Observaciones { get; set; }

    // Alerta de desnutrición (Nivel 1 / RN-12)
    public bool AlertaDesnutricion { get; set; } = false;
    public decimal? PorcentajeDisminucionPeso { get; set; }
    public decimal? PesoReferenciaUtilizado { get; set; }

    // Evaluación de aptitud para vacunación y vacunas (RN-03, RN-04)
    public bool EsAptoVacunacion { get; set; }
    public string? AptitudVacunacionResultado { get; set; }
    public string? MotivoNoAptoVacunacion { get; set; }
    public bool? SinFiebre { get; set; }
    public bool? BuenEstadoGeneral { get; set; }
    public bool? SinEnfermedadInfecciosaAguda { get; set; }
    public bool? AutorizacionVeterinaria { get; set; }
    public string? VacunaAplicada { get; set; }
    public string? VacunaDosis { get; set; }
    public string? VacunaLote { get; set; }
    public DateTime? VacunaProximaDosis { get; set; }

    // Navegación
    public Mascota Mascota { get; set; } = null!;
    public Usuario? Veterinario { get; set; }
    public ProcedimientoQuirurgico? ProcedimientoQuirurgico { get; set; }
    public ICollection<PrescripcionMedica> Prescripciones { get; set; } = new List<PrescripcionMedica>();
    public ICollection<EstudioComplementario> EstudiosComplementarios { get; set; } = new List<EstudioComplementario>();
    public ICollection<Turno> TurnosSeguimiento { get; set; } = new List<Turno>();
}
