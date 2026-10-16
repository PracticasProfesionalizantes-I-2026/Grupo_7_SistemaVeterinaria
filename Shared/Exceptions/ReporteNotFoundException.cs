namespace Shared.Exceptions;

public class ReporteNotFoundException : Exception
{
    public ReporteNotFoundException(Guid id)
        : base($"El reporte con ID '{id}' no fue encontrado.")
    {
    }

    public ReporteNotFoundException(string message)
        : base(message)
    {
    }
}
