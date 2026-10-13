namespace DataAccess.Repositories;

public interface ITokenRevocadoRepository
{
    Task<bool> EstaRevocadoAsync(string jti);
    Task RevocarAsync(string jti, Guid usuarioId, DateTime fechaExpiracion);
}
