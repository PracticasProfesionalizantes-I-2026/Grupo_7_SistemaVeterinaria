using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IHistorialPropietarioRepository
{
    Task<HistorialPropietario> CreateAsync(HistorialPropietario historial);
    Task<IEnumerable<HistorialPropietario>> GetByMascotaIdAsync(Guid mascotaId);
}
