namespace Shared.Exceptions;

public class VeterinarioNotFoundException : Exception
{
    public VeterinarioNotFoundException(Guid id)
        : base($"El profesional veterinario con ID '{id}' no fue encontrado, no se encuentra activo o no posee rol de Veterinario.")
    {
    }

    public VeterinarioNotFoundException(string message) : base(message)
    {
    }
}
