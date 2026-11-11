namespace Shared.Exceptions;

public class TurnoNoPendienteException : Exception
{
    public TurnoNoPendienteException(Guid id, string estadoActual)
        : base($"No es posible confirmar el turno porque se encuentra en estado '{estadoActual}'. Únicamente pueden confirmarse turnos en estado 'Pendiente'.")
    {
    }

    public TurnoNoPendienteException(string message) : base(message)
    {
    }
}

