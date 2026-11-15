namespace Shared.Exceptions;

public class TurnoMascotaNoCoincideException : Exception
{
    public TurnoMascotaNoCoincideException(Guid turnoId, Guid mascotaId)
        : base("La mascota de la atención médica no coincide con la mascota asociada al turno indicado.")
    {
    }

    public TurnoMascotaNoCoincideException(string message) : base(message)
    {
    }
}

