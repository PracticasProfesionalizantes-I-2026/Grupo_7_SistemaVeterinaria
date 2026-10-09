namespace Shared.Exceptions;

public class DniDuplicadoException : Exception
{
    public DniDuplicadoException(string dni)
        : base($"Ya existe un dueño registrado con el DNI '{dni}'.")
    {
    }
}
