namespace DataAccess.Entities;

public class TokenRevocado
{
    public Guid Id { get; set; }
    public string Jti { get; set; } = string.Empty;
    public Guid UsuarioId { get; set; }
    public DateTime FechaRevocacion { get; set; } = DateTime.UtcNow;
    public DateTime FechaExpiracion { get; set; }
}
