namespace Shared.Exceptions;

public class DuenoConMascotasException : Exception
{
    public DuenoConMascotasException(Guid duenoId)
        : base($"No se puede eliminar el dueño '{duenoId}' porque tiene mascotas registradas a su cargo.")
    {
    }
}
