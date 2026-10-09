using Shared.DTOs.Mascota;

namespace BusinessLogic.Interfaces;

public interface IMascotaService
{
    Task<IEnumerable<MascotaResponseDTO>> GetAllAsync(bool? soloActivas = null);
    Task<MascotaResponseDTO> GetByIdAsync(Guid id);
    Task<IEnumerable<MascotaResponseDTO>> GetByDuenoIdAsync(Guid duenoId);
    Task<MascotaResponseDTO> CreateAsync(MascotaCreateDTO dto);
    Task<MascotaResponseDTO> UpdateAsync(Guid id, MascotaUpdateDTO dto);
    Task<MascotaResponseDTO> DesactivarAsync(Guid id);
}
