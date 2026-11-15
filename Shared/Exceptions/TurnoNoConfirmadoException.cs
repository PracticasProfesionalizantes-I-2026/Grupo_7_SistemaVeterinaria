namespace Shared.Exceptions;

public class TurnoNoConfirmadoException : Exception
{
    public TurnoNoConfirmadoException(Guid id, string estadoActual)
        : base($"No es posible iniciar la atención del turno porque se encuentra en estado '{estadoActual}'. Únicamente puede iniciarse la atención de turnos en estado 'Confirmado'.")
    {
    }

    public TurnoNoConfirmadoException(string message) : base(message)
    {
    }
}

