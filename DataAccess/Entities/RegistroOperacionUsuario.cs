namespace DataAccess.Entities;

public class RegistroOperacionUsuario
{
    public Guid Id { get; set; }
    public Guid UsuarioId { get; set; }
    public string UsernameAfectado { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public Guid AdministradorId { get; set; }
    public string AdministradorUsername { get; set; } = string.Empty;
    public DateTime FechaOperacion { get; set; } = DateTime.UtcNow;
    public string? Detalles { get; set; }
}
