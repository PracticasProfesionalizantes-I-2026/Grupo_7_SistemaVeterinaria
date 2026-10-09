using Shared.DTOs.Dueno;

namespace BusinessLogic.Interfaces;

public interface IDuenoService
{
    Task<IEnumerable<DuenoResponseDTO>> GetAllAsync();
    Task<DuenoResponseDTO> GetByIdAsync(Guid id);
    Task<DuenoResponseDTO> CreateAsync(DuenoCreateDTO dto);
    Task<DuenoResponseDTO> UpdateAsync(Guid id, DuenoUpdateDTO dto);
    Task<bool> DeleteAsync(Guid id);
}
