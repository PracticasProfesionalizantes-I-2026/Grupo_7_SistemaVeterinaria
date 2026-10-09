using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IMascotaRepository
{
    Task<IEnumerable<Mascota>> GetAllAsync(bool? soloActivas = null);
    Task<Mascota?> GetByIdAsync(Guid id);
    Task<Mascota?> GetByIdTrackedAsync(Guid id);
    Task<IEnumerable<Mascota>> GetByDuenoIdAsync(Guid duenoId);
    Task<Mascota> CreateAsync(Mascota mascota);
    Task<Mascota> UpdateAsync(Mascota mascota);
    Task<bool> UpdateUltimaVisitaAsync(Guid mascotaId, DateTime fechaVisita);
}
