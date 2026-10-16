using Shared.DTOs.HistoriaClinica;

namespace BusinessLogic.Interfaces;

public interface IHistoriaClinicaService
{
    Task<HistoriaClinicaResponseDTO> GetHistoriaClinicaByMascotaIdAsync(Guid mascotaId);
    Task<IEnumerable<HistoriaClinicaBusquedaDTO>> BuscarHistoriasClinicasAsync(string? criterio);
}
