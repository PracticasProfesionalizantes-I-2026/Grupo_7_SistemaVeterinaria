using Shared.DTOs.Reporte;

namespace BusinessLogic.Interfaces;

public interface IReporteService
{
    Task<ReporteResponseDTO> GenerarReporteAsync(GenerarReporteRequestDTO dto, Guid usuarioId, string username);
    Task<IEnumerable<ReporteResumenDTO>> GetAllReportesAsync();
    Task<ReporteResponseDTO> GetReporteByIdAsync(Guid id);
}
