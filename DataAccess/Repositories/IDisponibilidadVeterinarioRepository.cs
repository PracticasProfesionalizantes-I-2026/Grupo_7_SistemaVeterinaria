using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IDisponibilidadVeterinarioRepository
{
    Task<IEnumerable<DisponibilidadVeterinario>> GetByVeterinarioIdAsync(Guid veterinarioId);
    Task<IEnumerable<DisponibilidadVeterinario>> GetByVeterinarioAndDiaAsync(Guid veterinarioId, DayOfWeek diaSemana);
    Task SetDisponibilidadesAsync(Guid veterinarioId, IEnumerable<DisponibilidadVeterinario> disponibilidades);
    Task<bool> EstaDentroDeDisponibilidadAsync(Guid veterinarioId, DateTime fechaHora, int duracionMinutos = 30);
}
