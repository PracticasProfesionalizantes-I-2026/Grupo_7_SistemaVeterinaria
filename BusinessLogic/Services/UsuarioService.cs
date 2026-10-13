using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Usuario;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;

    public UsuarioService(IUsuarioRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync(string? username = null, string? rol = null, string? estado = null)
    {
        var usuarios = await _usuarioRepository.GetAllAsync(username, rol, estado);
        var result = new List<UsuarioResponseDTO>();

        foreach (var usuario in usuarios)
        {
            result.Add(MapToResponseDTO(usuario));
        }

        return result;
    }

    public async Task<UsuarioResponseDTO> GetByIdAsync(Guid id)
    {
        var usuario = await _usuarioRepository.GetByIdAsync(id);
        if (usuario == null)
        {
            throw new UsuarioNotFoundException(id);
        }

        return MapToResponseDTO(usuario);
    }

    public async Task<UsuarioResponseDTO> DesbloquearCuentaAsync(Guid id, Guid administradorId, string administradorUsername)
    {
        var usuario = await _usuarioRepository.GetByIdTrackedAsync(id);
        if (usuario == null)
        {
            throw new UsuarioNotFoundException(id);
        }

        if (!usuario.Activo)
        {
            throw new UsuarioInactivoException("La cuenta de usuario se encuentra inactiva y no puede ser desbloqueada.");
        }

        if (!usuario.Bloqueado)
        {
            throw new InvalidOperationException("La cuenta no se encuentra bloqueada.");
        }

        // Restablecer estado sin modificar PasswordHash ni RequiereCambioPassword
        usuario.Bloqueado = false;
        usuario.IntentosFallidos = 0;

        // Registrar auditoría de la operación
        var registroAuditoria = new RegistroOperacionUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            UsernameAfectado = usuario.Username,
            TipoOperacion = "Desbloqueo",
            AdministradorId = administradorId,
            AdministradorUsername = administradorUsername,
            FechaOperacion = DateTime.UtcNow,
            Detalles = "Desbloqueo administrativo de cuenta y restablecimiento de contador de intentos fallidos a cero."
        };

        // Persistir usuario y auditoría atómicamente en una misma transacción
        await _usuarioRepository.DesbloquearConAuditoriaAsync(usuario, registroAuditoria);

        return MapToResponseDTO(usuario);
    }

    private static UsuarioResponseDTO MapToResponseDTO(Usuario usuario)
    {
        // Precedencia de estados: Priorizar Inactivo cuando Activo == false
        string estado;
        if (!usuario.Activo)
        {
            estado = "Inactivo";
        }
        else if (usuario.Bloqueado)
        {
            estado = "Bloqueado";
        }
        else
        {
            estado = "Activo";
        }

        return new UsuarioResponseDTO
        {
            Id = usuario.Id,
            Username = usuario.Username,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Estado = estado,
            Activo = usuario.Activo,
            Bloqueado = usuario.Bloqueado,
            IntentosFallidos = usuario.IntentosFallidos,
            RequiereCambioPassword = usuario.RequiereCambioPassword,
            UltimoAcceso = usuario.UltimoAcceso,
            FechaCreacion = usuario.FechaCreacion,
            PuedeDesbloquear = usuario.Activo && usuario.Bloqueado,
            PuedeRestablecerPassword = true
        };
    }
}
