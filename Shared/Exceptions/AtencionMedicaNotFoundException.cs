namespace Shared.Exceptions;

public class AtencionMedicaNotFoundException : Exception
{
    public AtencionMedicaNotFoundException(Guid id) 
        : base("No se encontró la atención médica solicitada.")
    {
    }

    public AtencionMedicaNotFoundException(string message) : base(message)
    {
    }
}
