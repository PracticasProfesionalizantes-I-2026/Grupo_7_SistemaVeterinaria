namespace Shared.Exceptions;

public class TurnoEnAtencionException : Exception
{
    public TurnoEnAtencionException(Guid id)
        : base("No es posible modificar ni cancelar el turno porque se encuentra en estado EnAtencion.")
    {
    }

    public TurnoEnAtencionException(string message) : base(message)
    {
    }
}

