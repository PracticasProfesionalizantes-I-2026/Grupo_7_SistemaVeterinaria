namespace Shared.DTOs.AtencionMedica;

public class AtencionMedicaResponseDTO
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public string NombreMascota { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public decimal PesoRegistrado { get; set; }
    public bool EsAptoVacunacion { get; set; }
    public string? VacunaAplicada { get; set; }
    public string? Observaciones { get; set; }
}
