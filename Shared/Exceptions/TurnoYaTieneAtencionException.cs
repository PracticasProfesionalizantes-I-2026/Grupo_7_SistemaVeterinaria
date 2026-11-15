namespace Shared.Exceptions;

public class TurnoYaTieneAtencionException : Exception
{
    public TurnoYaTieneAtencionException(Guid turnoId)
        : base("El turno indicado ya posee una atención médica registrada y vinculada.")
    {
    }

    public TurnoYaTieneAtencionException(string message) : base(message)
    {
    }
}

