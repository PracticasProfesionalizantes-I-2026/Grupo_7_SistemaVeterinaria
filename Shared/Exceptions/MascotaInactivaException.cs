namespace Shared.Exceptions;

public class MascotaInactivaException : Exception
{
    public MascotaInactivaException(Guid id)
        : base($"La mascota con identificador '{id}' está inactiva y no puede recibir nuevas atenciones médicas ni turnos.")
    {
    }
}
