using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.AtencionMedica;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class AtencionMedicaService : IAtencionMedicaService
{
    private readonly IAtencionMedicaRepository _atencionMedicaRepository;
    private readonly IMascotaRepository _mascotaRepository;

    public AtencionMedicaService(
        IAtencionMedicaRepository atencionMedicaRepository, 
        IMascotaRepository mascotaRepository)
    {
        _atencionMedicaRepository = atencionMedicaRepository;
        _mascotaRepository = mascotaRepository;
    }

    public async Task<IEnumerable<AtencionMedicaResponseDTO>> GetAllAsync()
    {
        var atenciones = await _atencionMedicaRepository.GetAllAsync();
        var result = new List<AtencionMedicaResponseDTO>();
        foreach (var a in atenciones)
        {
            result.Add(MapToResponseDTO(a));
        }
        return result;
    }

    public async Task<AtencionMedicaResponseDTO> GetByIdAsync(Guid id)
    {
        var atencion = await _atencionMedicaRepository.GetByIdAsync(id);
        if (atencion == null)
        {
            throw new AtencionMedicaNotFoundException(id);
        }

        return MapToResponseDTO(atencion);
    }

    public async Task<IEnumerable<AtencionMedicaResponseDTO>> GetByMascotaIdAsync(Guid mascotaId)
    {
        var mascota = await _mascotaRepository.GetByIdAsync(mascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(mascotaId);
        }

        var atenciones = await _atencionMedicaRepository.GetByMascotaIdAsync(mascotaId);
        var result = new List<AtencionMedicaResponseDTO>();
        foreach (var a in atenciones)
        {
            result.Add(MapToResponseDTO(a));
        }
        return result;
    }

    public async Task<AtencionMedicaResponseDTO> CreateAsync(AtencionMedicaCreateDTO dto)
    {
        ValidateCreateDto(dto);

        // Verificar existencia de la mascota
        var mascota = await _mascotaRepository.GetByIdAsync(dto.MascotaId);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(dto.MascotaId);
        }

        // RN-07: Una mascota inactiva no puede recibir nuevas atenciones médicas
        if (!mascota.Activa)
        {
            throw new MascotaInactivaException(dto.MascotaId);
        }

        // RN-03 y RN-04: La vacuna solo puede registrarse si el paciente es APTO
        if (!string.IsNullOrWhiteSpace(dto.VacunaAplicada) && !dto.EsAptoVacunacion)
        {
            throw new PacienteNoAptoVacunacionException(dto.MascotaId);
        }

        var nuevaAtencion = new AtencionMedica
        {
            MascotaId = dto.MascotaId,
            Fecha = DateTime.UtcNow,
            MotivoConsulta = dto.MotivoConsulta.Trim(),
            Diagnostico = dto.Diagnostico.Trim(),
            Tratamiento = dto.Tratamiento.Trim(),
            PesoRegistrado = dto.PesoRegistrado,
            EsAptoVacunacion = dto.EsAptoVacunacion,
            VacunaAplicada = string.IsNullOrWhiteSpace(dto.VacunaAplicada) ? null : dto.VacunaAplicada.Trim(),
            Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim()
        };

        var atencionCreada = await _atencionMedicaRepository.CreateAsync(nuevaAtencion);

        // Actualización explícita y automática de la fecha de última visita de la mascota
        await _mascotaRepository.UpdateUltimaVisitaAsync(dto.MascotaId, atencionCreada.Fecha);

        atencionCreada.Mascota = mascota;
        return MapToResponseDTO(atencionCreada);
    }

    // RN-01: Inmutabilidad de las atenciones médicas (no modificable)
    public Task UpdateAsync(Guid id)
    {
        throw new AtencionInmutableException(id);
    }

    // RN-01: Inmutabilidad de las atenciones médicas (no eliminable)
    public Task DeleteAsync(Guid id)
    {
        throw new AtencionInmutableException(id);
    }

    private static void ValidateCreateDto(AtencionMedicaCreateDTO dto)
    {
        if (dto.MascotaId == Guid.Empty)
            throw new ValidationException("El identificador de la mascota es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.MotivoConsulta))
            throw new ValidationException("El motivo de consulta es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Diagnostico))
            throw new ValidationException("El diagnóstico médico es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Tratamiento))
            throw new ValidationException("El tratamiento médico es obligatorio.");
        if (dto.PesoRegistrado <= 0)
            throw new ValidationException("El peso registrado en la consulta debe ser mayor a 0 kg.");
    }

    private AtencionMedicaResponseDTO MapToResponseDTO(AtencionMedica atencion)
    {
        return new AtencionMedicaResponseDTO
        {
            Id = atencion.Id,
            MascotaId = atencion.MascotaId,
            NombreMascota = atencion.Mascota != null ? atencion.Mascota.Nombre : string.Empty,
            Fecha = atencion.Fecha,
            MotivoConsulta = atencion.MotivoConsulta,
            Diagnostico = atencion.Diagnostico,
            Tratamiento = atencion.Tratamiento,
            PesoRegistrado = atencion.PesoRegistrado,
            EsAptoVacunacion = atencion.EsAptoVacunacion,
            VacunaAplicada = atencion.VacunaAplicada,
            Observaciones = atencion.Observaciones
        };
    }
}
