using Shared.DTOs.Turno;

namespace BusinessLogic.Interfaces;

public interface ITurnoService
{
    // Consultas
    Task<TurnoResponseDTO> GetByIdAsync(Guid id);
    Task<IEnumerable<TurnoResponseDTO>> GetAgendaAsync(AgendaFilterDTO filtro);

    // Operaciones sobre turnos individuales
    Task<TurnoResponseDTO> CreateManualAsync(TurnoCreateDTO dto);
    Task<TurnoResponseDTO> UpdateAsync(Guid id, TurnoUpdateDTO dto);
    Task<TurnoResponseDTO> CancelarAsync(Guid id, TurnoCancelarDTO dto, Guid usuarioId, string username);

    // Disponibilidad de veterinarios
    Task<IEnumerable<DisponibilidadVeterinarioDTO>> GetDisponibilidadVeterinarioAsync(Guid veterinarioId);
    Task<IEnumerable<DisponibilidadVeterinarioDTO>> ConfigurarDisponibilidadVeterinarioAsync(Guid veterinarioId, ConfigurarDisponibilidadDTO dto);

    // Seguimientos crónicos (Servicio reutilizable para consumo interno / CU-04)
    Task<GenerarTurnosCronicosResponseDTO> GenerarTurnosSeguimientoCronicoAsync(GenerarTurnosCronicosDTO dto);
}
