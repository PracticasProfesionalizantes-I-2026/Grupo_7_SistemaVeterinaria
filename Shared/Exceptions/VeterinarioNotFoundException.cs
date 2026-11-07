namespace Shared.Exceptions;

public class VeterinarioNotFoundException : Exception
{
    public VeterinarioNotFoundException(Guid id)
        : base("El profesional veterinario especificado no existe o no se encuentra disponible.")
    {
    }

    public VeterinarioNotFoundException(string message) : base(message)
    {
    }
}
