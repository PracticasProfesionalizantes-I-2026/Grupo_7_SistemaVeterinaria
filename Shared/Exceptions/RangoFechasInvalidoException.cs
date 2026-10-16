namespace Shared.Exceptions;

public class RangoFechasInvalidoException : Exception
{
    public RangoFechasInvalidoException(string message = "La fecha inicial no puede ser posterior a la fecha final.")
        : base(message)
    {
    }
}
