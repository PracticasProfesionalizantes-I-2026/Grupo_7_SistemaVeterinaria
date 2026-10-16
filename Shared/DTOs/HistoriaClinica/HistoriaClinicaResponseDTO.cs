using Shared.DTOs.AtencionMedica;

namespace Shared.DTOs.HistoriaClinica;

public class HistoriaClinicaResponseDTO
{
    // Identificación y datos generales de la mascota
    public Guid MascotaId { get; set; }
    public string NombreMascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public string Sexo { get; set; } = string.Empty;
    public DateTime FechaNacimiento { get; set; }
    public string Edad { get; set; } = string.Empty;
    public decimal? PesoActual { get; set; }
    public bool Activa { get; set; }
    public string? ObservacionesMascota { get; set; }

    // Fecha de última visita (INF-07 / Flujo 3b)
    public DateTime? FechaUltimaVisita { get; set; }
    public string UltimaVisitaVisualizada { get; set; } = "Sin visitas registradas";

    // Datos del dueño actual
    public Guid DuenoId { get; set; }
    public string DuenoNombreCompleto { get; set; } = string.Empty;
    public string DuenoDni { get; set; } = string.Empty;
    public string DuenoTelefono { get; set; } = string.Empty;
    public string DuenoEmail { get; set; } = string.Empty;

    // Historial clínico cronológico (RN-01, RN-02, INF-06)
    public List<AtencionMedicaResponseDTO> AtencionesMedicas { get; set; } = new();

    // Métricas y resumen clínico
    public int TotalAtenciones { get; set; }
    public int TotalCirugias { get; set; }
    public int TotalVacunasAplicadas { get; set; }

    // Mensaje informativo (ej. "Sin atenciones médicas registradas a la fecha." en Flujo 3b)
    public string? MensajeInformativo { get; set; }
}
