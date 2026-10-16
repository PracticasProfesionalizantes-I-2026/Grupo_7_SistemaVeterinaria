using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IReporteRepository
{
    Task<Reporte> CreateAsync(Reporte reporte);
    Task<Reporte?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reporte>> GetAllAsync();
}
