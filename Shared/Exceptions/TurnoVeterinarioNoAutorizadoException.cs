namespace Shared.Exceptions;

public class TurnoVeterinarioNoAutorizadoException : Exception
{
    public TurnoVeterinarioNoAutorizadoException(Guid turnoId, Guid veterinarioId)
        : base("Solo el veterinario asignado al turno está autorizado para ejecutar esta operación.")
    {
    }

    public TurnoVeterinarioNoAutorizadoException(string message) : base(message)
    {
    }
}

