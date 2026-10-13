namespace Shared.Exceptions;

public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() 
        : base("Nombre de usuario o contraseña incorrectos.")
    {
    }

    public CredencialesInvalidasException(string message) 
        : base(message)
    {
    }
}
