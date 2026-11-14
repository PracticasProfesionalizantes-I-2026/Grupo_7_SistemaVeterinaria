namespace Shared.Exceptions;

public class TurnoNoEnAtencionException : Exception
{
    public TurnoNoEnAtencionException(Guid id, string estadoActual)
        : base($"No es posible completar la operación porque el turno se encuentra en estado '{estadoActual}'. El turno debe encontrarse en estado 'EnAtencion'.")
    {
    }

    public TurnoNoEnAtencionException(string message) : base(message)
    {
    }
}

