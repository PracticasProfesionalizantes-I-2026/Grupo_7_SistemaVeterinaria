namespace DataAccess.Entities;

public class AtencionMedica
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string MotivoConsulta { get; set; } = string.Empty;
    public string Diagnostico { get; set; } = string.Empty;
    public string Tratamiento { get; set; } = string.Empty;
    public decimal PesoRegistrado { get; set; }
    public bool EsAptoVacunacion { get; set; }
    public string? VacunaAplicada { get; set; }
    public string? Observaciones { get; set; }

    // Navegación
    public Mascota Mascota { get; set; } = null!;
}
