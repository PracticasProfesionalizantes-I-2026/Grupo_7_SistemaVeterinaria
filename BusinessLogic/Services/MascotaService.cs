using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Mascota;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class MascotaService : IMascotaService
{
    private readonly IMascotaRepository _mascotaRepository;
    private readonly IDuenoRepository _duenoRepository;

    public MascotaService(IMascotaRepository mascotaRepository, IDuenoRepository duenoRepository)
    {
        _mascotaRepository = mascotaRepository;
        _duenoRepository = duenoRepository;
    }

    public async Task<IEnumerable<MascotaResponseDTO>> GetAllAsync(bool? soloActivas = null)
    {
        var mascotas = await _mascotaRepository.GetAllAsync(soloActivas);
        var result = new List<MascotaResponseDTO>();
        foreach (var m in mascotas)
        {
            result.Add(MapToResponseDTO(m));
        }
        return result;
    }

    public async Task<MascotaResponseDTO> GetByIdAsync(Guid id)
    {
        var mascota = await _mascotaRepository.GetByIdAsync(id);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(id);
        }

        return MapToResponseDTO(mascota);
    }

    public async Task<IEnumerable<MascotaResponseDTO>> GetByDuenoIdAsync(Guid duenoId)
    {
        var dueno = await _duenoRepository.GetByIdAsync(duenoId);
        if (dueno == null)
        {
            throw new DuenoNotFoundException(duenoId);
        }

        var mascotas = await _mascotaRepository.GetByDuenoIdAsync(duenoId);
        var result = new List<MascotaResponseDTO>();
        foreach (var m in mascotas)
        {
            result.Add(MapToResponseDTO(m));
        }
        return result;
    }

    public async Task<MascotaResponseDTO> CreateAsync(MascotaCreateDTO dto)
    {
        ValidateCreateDto(dto);

        // RN-06: Asociación obligatoria a un dueño registrado
        var dueno = await _duenoRepository.GetByIdAsync(dto.DuenoId);
        if (dueno == null)
        {
            throw new DuenoNotFoundException(dto.DuenoId);
        }

        var nuevaMascota = new Mascota
        {
            DuenoId = dto.DuenoId,
            Nombre = dto.Nombre.Trim(),
            Especie = dto.Especie.Trim(),
            Raza = dto.Raza.Trim(),
            Sexo = dto.Sexo.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Peso = dto.Peso,
            Activa = true,
            Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim(),
            FechaUltimaVisita = null
        };

        var mascotaCreada = await _mascotaRepository.CreateAsync(nuevaMascota);
        // Asociar referencia en memoria para el mapeo
        mascotaCreada.Dueno = dueno;

        return MapToResponseDTO(mascotaCreada);
    }

    public async Task<MascotaResponseDTO> UpdateAsync(Guid id, MascotaUpdateDTO dto)
    {
        ValidateUpdateDto(dto);

        var mascota = await _mascotaRepository.GetByIdTrackedAsync(id);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(id);
        }

        mascota.Nombre = dto.Nombre.Trim();
        mascota.Raza = dto.Raza.Trim();
        mascota.Sexo = dto.Sexo.Trim();
        mascota.Peso = dto.Peso;
        mascota.Observaciones = string.IsNullOrWhiteSpace(dto.Observaciones) ? null : dto.Observaciones.Trim();

        var mascotaActualizada = await _mascotaRepository.UpdateAsync(mascota);
        return MapToResponseDTO(mascotaActualizada);
    }

    public async Task<MascotaResponseDTO> DesactivarAsync(Guid id)
    {
        var mascota = await _mascotaRepository.GetByIdTrackedAsync(id);
        if (mascota == null)
        {
            throw new MascotaNotFoundException(id);
        }

        // RN-07 y MEJ-03: Si ya está inactiva, arrojar conflicto 409
        if (!mascota.Activa)
        {
            throw new MascotaYaInactivaException(id);
        }

        // Baja lógica: cambia estado a Inactiva conservando todos sus antecedentes
        mascota.Activa = false;
        var mascotaDesactivada = await _mascotaRepository.UpdateAsync(mascota);

        return MapToResponseDTO(mascotaDesactivada);
    }

    private static void ValidateCreateDto(MascotaCreateDTO dto)
    {
        if (dto.DuenoId == Guid.Empty)
            throw new ValidationException("El identificador del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre de la mascota es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Especie))
            throw new ValidationException("La especie de la mascota es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Raza))
            throw new ValidationException("La raza de la mascota es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Sexo))
            throw new ValidationException("El sexo de la mascota es obligatorio.");
        if (dto.Peso <= 0)
            throw new ValidationException("El peso de la mascota debe ser mayor a 0 kg.");
        if (dto.FechaNacimiento > DateTime.UtcNow)
            throw new ValidationException("La fecha de nacimiento no puede ser una fecha futura.");
    }

    private static void ValidateUpdateDto(MascotaUpdateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre de la mascota es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Raza))
            throw new ValidationException("La raza de la mascota es obligatoria.");
        if (string.IsNullOrWhiteSpace(dto.Sexo))
            throw new ValidationException("El sexo de la mascota es obligatorio.");
        if (dto.Peso <= 0)
            throw new ValidationException("El peso de la mascota debe ser mayor a 0 kg.");
    }

    private MascotaResponseDTO MapToResponseDTO(Mascota mascota)
    {
        return new MascotaResponseDTO
        {
            Id = mascota.Id,
            DuenoId = mascota.DuenoId,
            NombreDueno = mascota.Dueno != null ? $"{mascota.Dueno.Nombre} {mascota.Dueno.Apellido}" : string.Empty,
            Nombre = mascota.Nombre,
            Especie = mascota.Especie,
            Raza = mascota.Raza,
            Sexo = mascota.Sexo,
            FechaNacimiento = mascota.FechaNacimiento,
            Peso = mascota.Peso,
            Activa = mascota.Activa,
            FechaUltimaVisita = mascota.FechaUltimaVisita,
            Observaciones = mascota.Observaciones
        };
    }
}
