namespace Shared.Exceptions;

public class AtencionInmutableException : Exception
{
    public AtencionInmutableException(Guid id)
        : base($"La atención médica con identificador '{id}' es inmutable y no puede ser modificada ni eliminada.")
    {
    }
}
