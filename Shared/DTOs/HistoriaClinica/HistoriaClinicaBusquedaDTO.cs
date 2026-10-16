namespace Shared.DTOs.HistoriaClinica;

public class HistoriaClinicaBusquedaDTO
{
    public Guid MascotaId { get; set; }
    public string NombreMascota { get; set; } = string.Empty;
    public string Especie { get; set; } = string.Empty;
    public string Raza { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public Guid DuenoId { get; set; }
    public string DuenoNombreCompleto { get; set; } = string.Empty;
    public string DuenoDni { get; set; } = string.Empty;
    public DateTime? FechaUltimaVisita { get; set; }
    public string UltimaVisitaVisualizada { get; set; } = "Sin visitas registradas";
    public int TotalAtenciones { get; set; }
}
