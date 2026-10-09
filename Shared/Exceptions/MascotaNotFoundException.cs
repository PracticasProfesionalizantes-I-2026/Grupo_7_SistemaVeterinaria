namespace Shared.Exceptions;

public class MascotaNotFoundException : Exception
{
    public MascotaNotFoundException(Guid id) 
        : base($"No se encontró la mascota con identificador '{id}'.")
    {
    }

    public MascotaNotFoundException(string message) : base(message)
    {
    }
}
