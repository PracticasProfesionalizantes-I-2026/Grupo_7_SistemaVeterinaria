namespace Shared.Exceptions;

public class MascotaYaInactivaException : Exception
{
    public MascotaYaInactivaException(Guid id)
        : base($"La mascota con identificador '{id}' ya se encuentra inactiva.")
    {
    }
}
