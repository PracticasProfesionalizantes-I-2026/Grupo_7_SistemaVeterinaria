using System.Security.Cryptography;
using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Usuario;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasherService _passwordHasher;

    public UsuarioService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasherService passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
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

    public async Task<RestablecerPasswordResponseDTO> RestablecerPasswordAsync(Guid id, Guid administradorId, string administradorUsername)
    {
        var usuario = await _usuarioRepository.GetByIdTrackedAsync(id);
        if (usuario == null)
        {
            throw new UsuarioNotFoundException(id);
        }

        // Opción A: Cuentas inactivas no admiten restablecimiento de contraseña
        if (!usuario.Activo)
        {
            throw new UsuarioInactivoException("La cuenta de usuario se encuentra inactiva y no puede recibir un restablecimiento de contraseña.");
        }

        // Generar contraseña temporal segura que cumpla con la política de contraseñas
        var passwordTemporal = GenerarPasswordTemporalSegura();

        // Almacenar únicamente el hash de la contraseña temporal y exigir cambio obligatorio
        usuario.PasswordHash = _passwordHasher.HashPassword(passwordTemporal);
        usuario.RequiereCambioPassword = true;

        // NOTA: No modificar usuario.Bloqueado ni usuario.IntentosFallidos (CU-14)

        // Registrar auditoría de la operación SIN incluir la contraseña temporal
        var registroAuditoria = new RegistroOperacionUsuario
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            UsernameAfectado = usuario.Username,
            TipoOperacion = "Restablecimiento de contraseña",
            AdministradorId = administradorId,
            AdministradorUsername = administradorUsername,
            FechaOperacion = DateTime.UtcNow,
            Detalles = "Restablecimiento administrativo de contraseña con generación de contraseña temporal y cambio obligatorio."
        };

        // Persistir usuario y auditoría atómicamente en una misma transacción
        await _usuarioRepository.RestablecerPasswordConAuditoriaAsync(usuario, registroAuditoria);

        var mensaje = usuario.Bloqueado
            ? "Contraseña restablecida exitosamente. La cuenta permanece bloqueada y requerirá ser desbloqueada antes de que el usuario pueda ingresar."
            : "Contraseña restablecida exitosamente. El usuario deberá cambiarla obligatoriamente en su próximo inicio de sesión.";

        return new RestablecerPasswordResponseDTO
        {
            UsuarioId = usuario.Id,
            Username = usuario.Username,
            PasswordTemporal = passwordTemporal,
            RequiereCambioPassword = true,
            Bloqueado = usuario.Bloqueado,
            Mensaje = mensaje,
            Advertencia = "Comunique la contraseña temporal al usuario por un medio seguro. Esta contraseña no se volverá a mostrar ni se almacena en texto plano."
        };
    }

    private static string GenerarPasswordTemporalSegura()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnopqrstuvwxyz";
        const string numeros = "23456789";
        const string especiales = "!@#$%&*";
        const string todos = mayusculas + minusculas + numeros + especiales;

        var chars = new List<char>
        {
            mayusculas[RandomNumberGenerator.GetInt32(mayusculas.Length)],
            minusculas[RandomNumberGenerator.GetInt32(minusculas.Length)],
            numeros[RandomNumberGenerator.GetInt32(numeros.Length)],
            especiales[RandomNumberGenerator.GetInt32(especiales.Length)]
        };

        for (int i = chars.Count; i < 10; i++)
        {
            chars.Add(todos[RandomNumberGenerator.GetInt32(todos.Length)]);
        }

        for (int i = chars.Count - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars.ToArray());
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
            PuedeRestablecerPassword = usuario.Activo
        };
    }
}
