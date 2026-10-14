namespace DataAccess.Entities;

public class HistorialPropietario
{
    public Guid Id { get; set; }
    public Guid MascotaId { get; set; }
    public Guid DuenoAnteriorId { get; set; }
    public Guid NuevoDuenoId { get; set; }
    public DateTime FechaReasignacion { get; set; } = DateTime.UtcNow;
    public Guid UsuarioId { get; set; }
    public string UsuarioUsername { get; set; } = string.Empty;

    // Navegación
    public Mascota Mascota { get; set; } = null!;
    public Dueno DuenoAnterior { get; set; } = null!;
    public Dueno NuevoDueno { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
