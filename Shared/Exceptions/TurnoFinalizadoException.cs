namespace Shared.Exceptions;

public class TurnoFinalizadoException : Exception
{
    public TurnoFinalizadoException(Guid id)
        : base($"No es posible modificar ni cancelar el turno '{id}' porque se encuentra en estado Finalizado (RN-09).")
    {
    }

    public TurnoFinalizadoException(string message) : base(message)
    {
    }
}
