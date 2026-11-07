namespace Shared.Exceptions;

public class TurnoFinalizadoException : Exception
{
    public TurnoFinalizadoException(Guid id)
        : base("No es posible modificar ni cancelar el turno porque se encuentra en estado Finalizado.")
    {
    }

    public TurnoFinalizadoException(string message) : base(message)
    {
    }
}
