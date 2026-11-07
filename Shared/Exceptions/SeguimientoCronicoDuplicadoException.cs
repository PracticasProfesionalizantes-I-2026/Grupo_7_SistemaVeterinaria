namespace Shared.Exceptions;

public class SeguimientoCronicoDuplicadoException : Exception
{
    public SeguimientoCronicoDuplicadoException(Guid atencionMedicaId)
        : base("Ya existen turnos de seguimiento generados para la atención médica indicada.")
    {
    }

    public SeguimientoCronicoDuplicadoException(string message) : base(message)
    {
    }
}
