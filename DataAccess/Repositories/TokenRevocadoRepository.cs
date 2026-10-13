using DataAccess.Context;
using DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public class TokenRevocadoRepository : ITokenRevocadoRepository
{
    private readonly VeterinariaDbContext _context;

    public TokenRevocadoRepository(VeterinariaDbContext context)
    {
        _context = context;
    }

    public async Task<bool> EstaRevocadoAsync(string jti)
    {
        return await _context.TokensRevocados
            .AsNoTracking()
            .AnyAsync(t => t.Jti == jti);
    }

    public async Task RevocarAsync(string jti, Guid usuarioId, DateTime fechaExpiracion)
    {
        var tokenRevocado = new TokenRevocado
        {
            Id = Guid.NewGuid(),
            Jti = jti,
            UsuarioId = usuarioId,
            FechaRevocacion = DateTime.UtcNow,
            FechaExpiracion = fechaExpiracion
        };

        await _context.TokensRevocados.AddAsync(tokenRevocado);
        await _context.SaveChangesAsync();
    }
}
