using DataAccess.Entities;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Repositories;

public interface IAtencionMedicaRepository
{
    Task<IEnumerable<AtencionMedica>> GetAllAsync();
    Task<AtencionMedica?> GetByIdAsync(Guid id);
    Task<IEnumerable<AtencionMedica>> GetByMascotaIdAsync(Guid mascotaId);
    Task<AtencionMedica?> GetUltimaAtencionByMascotaIdAsync(Guid mascotaId);
    Task<bool> HasTurnosSeguimientoAsync(Guid atencionMedicaId);
    Task<AtencionMedica> CreateAsync(AtencionMedica atencionMedica);
    Task<IEnumerable<AtencionMedica>> GetByRangoFechasAsync(DateTime desde, DateTime hasta);
    Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken = default);
}
