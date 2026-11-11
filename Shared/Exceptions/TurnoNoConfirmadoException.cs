namespace Shared.Exceptions;

public class TurnoNoConfirmadoException : Exception
{
    public TurnoNoConfirmadoException(Guid id, string estadoActual)
        : base($"No es posible finalizar el turno porque se encuentra en estado '{estadoActual}'. Únicamente pueden finalizarse turnos en estado 'Confirmado'.")
    {
    }

    public TurnoNoConfirmadoException(string message) : base(message)
    {
    }
}

