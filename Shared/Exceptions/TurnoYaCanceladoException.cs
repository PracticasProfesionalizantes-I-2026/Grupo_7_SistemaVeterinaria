namespace Shared.Exceptions;

public class TurnoYaCanceladoException : Exception
{
    public TurnoYaCanceladoException(Guid id)
        : base("El turno ya fue cancelado previamente y no puede cancelarse ni reactivarse.")
    {
    }

    public TurnoYaCanceladoException(string message) : base(message)
    {
    }
}
