namespace Shared.Exceptions;

public class HorarioNoDisponibleException : Exception
{
    public HorarioNoDisponibleException(string message) : base(message)
    {
    }

    public HorarioNoDisponibleException(DateTime fechaHora)
        : base($"El horario '{fechaHora:yyyy-MM-dd HH:mm}' no se encuentra disponible para el profesional seleccionado.")
    {
    }
}
