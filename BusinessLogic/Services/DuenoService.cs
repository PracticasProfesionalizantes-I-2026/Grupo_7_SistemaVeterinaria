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

        var dniNormalizado = dto.Dni.Trim();

        // RN-05 (CU-08): Si el DNI pertenece a otro dueño distinto, lanzar DniDuplicadoException (409 Conflict)
        var duenoConMismoDni = await _duenoRepository.GetByDniAsync(dniNormalizado);
        if (duenoConMismoDni != null && duenoConMismoDni.Id != id)
        {
            throw new DniDuplicadoException(dniNormalizado);
        }

        dueno.Nombre = dto.Nombre.Trim();
        dueno.Apellido = dto.Apellido.Trim();
        dueno.Dni = dniNormalizado;
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
        if (dto == null)
            throw new ValidationException("La solicitud no puede ser nula.");

        ValidateCamposComunes(dto.Nombre, dto.Apellido, dto.Dni, dto.Telefono, dto.Email, dto.Direccion);
    }

    private static void ValidateUpdateDto(DuenoUpdateDTO dto)
    {
        if (dto == null)
            throw new ValidationException("La solicitud no puede ser nula.");

        ValidateCamposComunes(dto.Nombre, dto.Apellido, dto.Dni, dto.Telefono, dto.Email, dto.Direccion);
    }

    private static void ValidateCamposComunes(
        string? nombre,
        string? apellido,
        string? dni,
        string? telefono,
        string? email,
        string? direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ValidationException("El nombre del dueño es obligatorio.");
        if (nombre.Trim().Length > 100)
            throw new ValidationException("El nombre no puede superar los 100 caracteres.");

        if (string.IsNullOrWhiteSpace(apellido))
            throw new ValidationException("El apellido del dueño es obligatorio.");
        if (apellido.Trim().Length > 100)
            throw new ValidationException("El apellido no puede superar los 100 caracteres.");

        if (string.IsNullOrWhiteSpace(dni))
            throw new ValidationException("El DNI del dueño es obligatorio.");
        var dniTrimmed = dni.Trim();
        if (!dniTrimmed.All(char.IsDigit))
            throw new ValidationException("El DNI debe contener únicamente dígitos numéricos.");
        if (dniTrimmed.Length < 7 || dniTrimmed.Length > 10)
            throw new ValidationException("El DNI debe tener entre 7 y 10 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(telefono))
            throw new ValidationException("El teléfono de contacto es obligatorio.");
        var telefonoTrimmed = telefono.Trim();
        if (telefonoTrimmed.Length > 30)
            throw new ValidationException("El teléfono no puede superar los 30 caracteres.");
        if (!telefonoTrimmed.All(c => char.IsDigit(c) || c == ' ' || c == '-' || c == '+' || c == '(' || c == ')'))
            throw new ValidationException("El teléfono contiene caracteres inválidos.");
        if (telefonoTrimmed.Count(char.IsDigit) < 6)
            throw new ValidationException("El teléfono debe contener al menos 6 dígitos numéricos.");

        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException("El email de contacto es obligatorio.");
        var emailTrimmed = email.Trim();
        if (emailTrimmed.Length > 150)
            throw new ValidationException("El email no puede superar los 150 caracteres.");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(emailTrimmed))
            throw new ValidationException("El formato del correo electrónico es inválido.");

        if (direccion != null && direccion.Trim().Length > 200)
            throw new ValidationException("La dirección no puede superar los 200 caracteres.");
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
