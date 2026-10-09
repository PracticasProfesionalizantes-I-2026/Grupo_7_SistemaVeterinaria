namespace Shared.Exceptions;

public class AtencionMedicaNotFoundException : Exception
{
    public AtencionMedicaNotFoundException(Guid id) 
        : base($"No se encontró la atención médica con identificador '{id}'.")
    {
    }

    public AtencionMedicaNotFoundException(string message) : base(message)
    {
    }
}
