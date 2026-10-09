using Shared.DTOs.AtencionMedica;

namespace BusinessLogic.Interfaces;

public interface IAtencionMedicaService
{
    Task<IEnumerable<AtencionMedicaResponseDTO>> GetAllAsync();
    Task<AtencionMedicaResponseDTO> GetByIdAsync(Guid id);
    Task<IEnumerable<AtencionMedicaResponseDTO>> GetByMascotaIdAsync(Guid mascotaId);
    Task<AtencionMedicaResponseDTO> CreateAsync(AtencionMedicaCreateDTO dto);
    Task UpdateAsync(Guid id);
    Task DeleteAsync(Guid id);
}
