namespace Shared.Exceptions;

public class UsuarioNotFoundException : Exception
{
    public UsuarioNotFoundException(Guid id)
        : base("No se encontró el usuario solicitado.")
    {
    }

    public UsuarioNotFoundException(string message) : base(message)
    {
    }
}
