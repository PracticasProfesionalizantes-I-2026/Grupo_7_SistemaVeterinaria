namespace Shared.Exceptions;

public class TurnoNotFoundException : Exception
{
    public TurnoNotFoundException(Guid id)
        : base("El turno solicitado no fue encontrado en el sistema.")
    {
    }

    public TurnoNotFoundException(string message) : base(message)
    {
    }
}
