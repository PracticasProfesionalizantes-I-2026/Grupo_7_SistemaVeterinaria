using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface IAtencionMedicaRepository
{
    Task<IEnumerable<AtencionMedica>> GetAllAsync();
    Task<AtencionMedica?> GetByIdAsync(Guid id);
    Task<IEnumerable<AtencionMedica>> GetByMascotaIdAsync(Guid mascotaId);
    Task<AtencionMedica?> GetUltimaAtencionByMascotaIdAsync(Guid mascotaId);
    Task<bool> HasTurnosSeguimientoAsync(Guid atencionMedicaId);
    Task<AtencionMedica> CreateAsync(AtencionMedica atencionMedica);
}
