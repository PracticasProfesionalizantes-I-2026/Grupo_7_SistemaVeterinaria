using DataAccess.Entities;

namespace DataAccess.Repositories;

public interface ITurnoRepository
{
    Task<Turno?> GetByIdAsync(Guid id);
    Task<Turno?> GetByIdTrackedAsync(Guid id);
    Task<IEnumerable<Turno>> GetAgendaAsync(DateTime? fechaDesde, DateTime? fechaHasta, Guid? veterinarioId, string? estado);
    Task<bool> ExisteSuperposicionAsync(Guid veterinarioId, DateTime fechaHora, Guid? turnoIdExcluir = null);
    Task<bool> ExisteSeguimientoParaAtencionAsync(Guid atencionMedicaId);
    Task<Turno> CreateAsync(Turno turno);
    Task<Turno> UpdateAsync(Turno turno);
    Task CreateRangeAsync(IEnumerable<Turno> turnos);
}
