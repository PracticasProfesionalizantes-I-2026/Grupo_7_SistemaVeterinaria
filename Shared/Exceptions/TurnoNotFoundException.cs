namespace Shared.Exceptions;

public class TurnoNotFoundException : Exception
{
    public TurnoNotFoundException(Guid id)
        : base($"El turno con ID '{id}' no fue encontrado en el sistema.")
    {
    }

    public TurnoNotFoundException(string message) : base(message)
    {
    }
}
