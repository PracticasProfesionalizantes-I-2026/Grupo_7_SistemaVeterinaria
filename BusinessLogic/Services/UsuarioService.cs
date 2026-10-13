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
