namespace Shared.Exceptions;

public class CuentaNoBloqueadaException : Exception
{
    public CuentaNoBloqueadaException()
        : base("La cuenta no se encuentra bloqueada.")
    {
    }

    public CuentaNoBloqueadaException(string message)
        : base(message)
    {
    }
}

