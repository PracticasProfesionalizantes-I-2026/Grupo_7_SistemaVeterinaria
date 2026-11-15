namespace Shared.Exceptions;

public class TurnoSinAtencionMedicaException : Exception
{
    public TurnoSinAtencionMedicaException(Guid id)
        : base("No es posible finalizar el turno porque aún no posee una atención médica registrada y vinculada.")
    {
    }

    public TurnoSinAtencionMedicaException(string message) : base(message)
    {
    }
}

