using BusinessLogic.Interfaces;
using Shared.Security;

namespace BusinessLogic.Services;

public class PasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password)
    {
        return PasswordHelper.HashPassword(password);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return PasswordHelper.VerifyPassword(password, passwordHash);
    }
}
