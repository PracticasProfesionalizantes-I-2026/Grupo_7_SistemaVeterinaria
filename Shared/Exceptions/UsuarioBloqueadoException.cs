namespace Shared.Exceptions;

public class UsuarioBloqueadoException : Exception
{
    public UsuarioBloqueadoException() 
        : base("Usuario bloqueado. Contacte al Administrador.")
    {
    }

    public UsuarioBloqueadoException(string message) 
        : base(message)
    {
    }
}
