using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IDuenoRepository
{
    Task<IEnumerable<Dueno>> GetAllAsync();
    Task<Dueno?> GetByIdAsync(Guid id);
    Task<Dueno?> GetByIdTrackedAsync(Guid id);
    Task<Dueno?> GetByDniAsync(string dni);
    Task<bool> HasMascotasAsync(Guid duenoId);
    Task<Dueno> CreateAsync(Dueno dueno);
    Task<Dueno> UpdateAsync(Dueno dueno);
    Task<bool> DeleteAsync(Guid id);
}
