namespace Shared.Exceptions;

public class DuenoNotFoundException : Exception
{
    public DuenoNotFoundException(Guid id) 
        : base("No se encontró el dueño solicitado.")
    {
    }

    public DuenoNotFoundException(string message) : base(message)
    {
    }
}
