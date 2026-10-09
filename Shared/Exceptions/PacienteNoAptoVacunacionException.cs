namespace Shared.Exceptions;

public class PacienteNoAptoVacunacionException : Exception
{
    public PacienteNoAptoVacunacionException(Guid mascotaId)
        : base($"No se puede registrar una vacuna para la mascota '{mascotaId}' porque la evaluación clínica determinó que no es apta para vacunación.")
    {
    }
}
