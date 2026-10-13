using DataAccess.Entities;

namespace BusinessLogic.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(Usuario usuario);
}
