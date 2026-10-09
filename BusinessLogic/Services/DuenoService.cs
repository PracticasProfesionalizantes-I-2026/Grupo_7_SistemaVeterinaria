using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Dueno;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class DuenoService : IDuenoService
{
    private readonly IDuenoRepository _duenoRepository;

    public DuenoService(IDuenoRepository duenoRepository)
    {
        _duenoRepository = duenoRepository;
    }

    public async Task<IEnumerable<DuenoResponseDTO>> GetAllAsync()
    {
        var duenos = await _duenoRepository.GetAllAsync();
        var result = new List<DuenoResponseDTO>();
        foreach (var dueno in duenos)
        {
            result.Add(MapToResponseDTO(dueno));
        }
        return result;
    }

    public async Task<DuenoResponseDTO> GetByIdAsync(Guid id)
    {
        var dueno = await _duenoRepository.GetByIdAsync(id);
        if (dueno == null)
        {
            throw new DuenoNotFoundException(id);
        }

        return MapToResponseDTO(dueno);
    }

    public async Task<DuenoResponseDTO> CreateAsync(DuenoCreateDTO dto)
    {
        ValidateCreateDto(dto);

        // RN-05: Unicidad del DNI del Dueño
        var duenoExistente = await _duenoRepository.GetByDniAsync(dto.Dni.Trim());
        if (duenoExistente != null)
        {
            throw new DniDuplicadoException(dto.Dni.Trim());
        }

        var nuevoDueno = new Dueno
        {
            Nombre = dto.Nombre.Trim(),
            Apellido = dto.Apellido.Trim(),
            Dni = dto.Dni.Trim(),
            Telefono = dto.Telefono.Trim(),
            Email = dto.Email.Trim(),
            Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim()
        };

        var duenoCreado = await _duenoRepository.CreateAsync(nuevoDueno);
        return MapToResponseDTO(duenoCreado);
    }

    public async Task<DuenoResponseDTO> UpdateAsync(Guid id, DuenoUpdateDTO dto)
    {
        ValidateUpdateDto(dto);

        var dueno = await _duenoRepository.GetByIdTrackedAsync(id);
        if (dueno == null)
        {
            throw new DuenoNotFoundException(id);
        }

        dueno.Nombre = dto.Nombre.Trim();
        dueno.Apellido = dto.Apellido.Trim();
        dueno.Telefono = dto.Telefono.Trim();
        dueno.Email = dto.Email.Trim();
        dueno.Direccion = string.IsNullOrWhiteSpace(dto.Direccion) ? null : dto.Direccion.Trim();

        var duenoActualizado = await _duenoRepository.UpdateAsync(dueno);
        return MapToResponseDTO(duenoActualizado);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var dueno = await _duenoRepository.GetByIdAsync(id);
        if (dueno == null)
        {
            throw new DuenoNotFoundException(id);
        }

        // Restricción relacional: no eliminar si tiene mascotas
        if (await _duenoRepository.HasMascotasAsync(id))
        {
            throw new DuenoConMascotasException(id);
        }

        return await _duenoRepository.DeleteAsync(id);
    }

    private static void ValidateCreateDto(DuenoCreateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ValidationException("El apellido del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Dni))
            throw new ValidationException("El DNI del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Telefono))
            throw new ValidationException("El teléfono de contacto es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ValidationException("El email de contacto es obligatorio.");
    }

    private static void ValidateUpdateDto(DuenoUpdateDTO dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new ValidationException("El nombre del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Apellido))
            throw new ValidationException("El apellido del dueño es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Telefono))
            throw new ValidationException("El teléfono de contacto es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Email))
            throw new ValidationException("El email de contacto es obligatorio.");
    }

    private DuenoResponseDTO MapToResponseDTO(Dueno dueno)
    {
        return new DuenoResponseDTO
        {
            Id = dueno.Id,
            Nombre = dueno.Nombre,
            Apellido = dueno.Apellido,
            Dni = dueno.Dni,
            Telefono = dueno.Telefono,
            Email = dueno.Email,
            Direccion = dueno.Direccion,
            FechaRegistro = dueno.FechaRegistro,
            CantidadMascotas = dueno.Mascotas?.Count ?? 0
        };
    }
}
