namespace Shared.Exceptions;

public class UsuarioNotFoundException : Exception
{
    public UsuarioNotFoundException(Guid id)
        : base($"No se encontró el usuario con identificador '{id}'.")
    {
    }

    public UsuarioNotFoundException(string message) : base(message)
    {
    }
}
