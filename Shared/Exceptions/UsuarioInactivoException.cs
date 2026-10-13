namespace Shared.Exceptions;

public class UsuarioInactivoException : Exception
{
    public UsuarioInactivoException() 
        : base("El usuario no se encuentra habilitado para operar en el sistema.")
    {
    }

    public UsuarioInactivoException(string message) 
        : base(message)
    {
    }
}
