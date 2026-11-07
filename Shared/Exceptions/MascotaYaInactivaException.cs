namespace Shared.Exceptions;

public class MascotaYaInactivaException : Exception
{
    public MascotaYaInactivaException(Guid id)
        : base("La mascota ya se encuentra inactiva.")
    {
    }
}
