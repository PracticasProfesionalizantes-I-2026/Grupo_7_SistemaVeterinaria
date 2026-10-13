using BusinessLogic.Interfaces;
using DataAccess.Entities;
using DataAccess.Repositories;
using Shared.DTOs.Auth;
using Shared.Exceptions;

namespace BusinessLogic.Services;

public class AuthService : IAuthService
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITokenRevocadoRepository _tokenRevocadoRepository;

    public AuthService(
        IUsuarioRepository usuarioRepository,
        IPasswordHasherService passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ITokenRevocadoRepository tokenRevocadoRepository)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tokenRevocadoRepository = tokenRevocadoRepository;
    }

    public async Task<LoginResponseDTO> LoginAsync(LoginRequestDTO dto)
    {
        if (dto == null)
        {
            throw new ValidationException("La solicitud de inicio de sesión no puede ser nula.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioOEmail) || string.IsNullOrWhiteSpace(dto.Password))
        {
            throw new ValidationException("El nombre de usuario/correo y la contraseña son obligatorios.");
        }

        var usuario = await _usuarioRepository.GetByUsernameOrEmailTrackedAsync(dto.UsuarioOEmail);
        if (usuario == null)
        {
            // Evitar enumeración de usuarios
            throw new CredencialesInvalidasException("Nombre de usuario o contraseña incorrectos.");
        }

        // A3: Cuenta previamente bloqueada
        if (usuario.Bloqueado)
        {
            throw new UsuarioBloqueadoException("Usuario bloqueado. Contacte al Administrador.");
        }

        // A6: Cuenta inactiva o deshabilitada administrativamente
        if (!usuario.Activo)
        {
            throw new UsuarioInactivoException("El usuario no se encuentra habilitado para operar en el sistema.");
        }

        // Verificación de credenciales
        bool esPasswordValida = _passwordHasher.VerifyPassword(dto.Password, usuario.PasswordHash);
        if (!esPasswordValida)
        {
            usuario.IntentosFallidos++;

            // A2: Quinto intento fallido consecutivo bloquea automáticamente
            if (usuario.IntentosFallidos >= 5)
            {
                usuario.Bloqueado = true;
                await _usuarioRepository.UpdateAsync(usuario);
                throw new UsuarioBloqueadoException("Usuario bloqueado. Contacte al Administrador.");
            }

            // A1: Credenciales incorrectas antes del límite
            await _usuarioRepository.UpdateAsync(usuario);
            throw new CredencialesInvalidasException("Nombre de usuario o contraseña incorrectos.");
        }

        // Autenticación exitosa: restablecer contador de intentos a cero (A4 / Flujo principal)
        usuario.IntentosFallidos = 0;
        usuario.UltimoAcceso = DateTime.UtcNow;
        await _usuarioRepository.UpdateAsync(usuario);

        // A8: Detección de contraseña temporal con cambio obligatorio
        if (usuario.RequiereCambioPassword)
        {
            return new LoginResponseDTO
            {
                Token = null,
                Username = usuario.Username,
                Email = usuario.Email,
                Rol = usuario.Rol,
                RequiereCambioPassword = true,
                Mensaje = "Inicio de sesión con contraseña temporal. Debe cambiar su contraseña obligatoriamente antes de continuar."
            };
        }

        // Flujo principal: emisión de token JWT
        var token = _jwtTokenGenerator.GenerateToken(usuario);

        return new LoginResponseDTO
        {
            Token = token,
            Username = usuario.Username,
            Email = usuario.Email,
            Rol = usuario.Rol,
            RequiereCambioPassword = false,
            Mensaje = "Inicio de sesión exitoso."
        };
    }

    public async Task<LoginResponseDTO> CambiarPasswordTemporalAsync(CambiarPasswordTemporalDTO dto)
    {
        if (dto == null)
        {
            throw new ValidationException("La solicitud de cambio de contraseña no puede ser nula.");
        }

        if (string.IsNullOrWhiteSpace(dto.UsuarioOEmail))
        {
            throw new ValidationException("El nombre de usuario o correo electrónico es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(dto.PasswordActual))
        {
            throw new ValidationException("La contraseña actual es obligatoria.");
        }

        if (string.IsNullOrWhiteSpace(dto.NuevaPassword))
        {
            throw new ValidationException("La nueva contraseña es obligatoria.");
        }

        if (dto.NuevaPassword != dto.ConfirmacionPassword)
        {
            throw new ValidationException("La nueva contraseña y su confirmación no coinciden.");
        }

        ValidarComplejidadPassword(dto.NuevaPassword);

        var usuario = await _usuarioRepository.GetByUsernameOrEmailTrackedAsync(dto.UsuarioOEmail);
        if (usuario == null)
        {
            throw new CredencialesInvalidasException("El usuario especificado no existe.");
        }

        if (usuario.Bloqueado)
        {
            throw new UsuarioBloqueadoException("Usuario bloqueado. Contacte al Administrador.");
        }

        if (!usuario.Activo)
        {
            throw new UsuarioInactivoException("El usuario no se encuentra habilitado para operar en el sistema.");
        }

        bool esPasswordActualValida = _passwordHasher.VerifyPassword(dto.PasswordActual, usuario.PasswordHash);
        if (!esPasswordActualValida)
        {
            throw new CredencialesInvalidasException("La contraseña actual es incorrecta.");
        }

        // Actualizar contraseña y desactivar condición de cambio obligatorio
        usuario.PasswordHash = _passwordHasher.HashPassword(dto.NuevaPassword);
        usuario.RequiereCambioPassword = false;
        usuario.IntentosFallidos = 0;
        usuario.UltimoAcceso = DateTime.UtcNow;

        await _usuarioRepository.UpdateAsync(usuario);

        var token = _jwtTokenGenerator.GenerateToken(usuario);

        return new LoginResponseDTO
        {
            Token = token,
            Username = usuario.Username,
            Email = usuario.Email,
            Rol = usuario.Rol,
            RequiereCambioPassword = false,
            Mensaje = "Contraseña actualizada exitosamente. Acceso concedido."
        };
    }

    private static void ValidarComplejidadPassword(string password)
    {
        if (password.Length < 8)
        {
            throw new ValidationException("La nueva contraseña debe tener al menos 8 caracteres.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new ValidationException("La nueva contraseña debe contener al menos una letra mayúscula.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new ValidationException("La nueva contraseña debe contener al menos una letra minúscula.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new ValidationException("La nueva contraseña debe contener al menos un número.");
        }
    }

    public async Task LogoutAsync(string jti, Guid usuarioId, DateTime? fechaExpiracion = null)
    {
        if (string.IsNullOrWhiteSpace(jti))
        {
            return;
        }

        // Flujo alternativo 3a: Cierre de sesión idempotente si el token ya se encontraba revocado
        if (await _tokenRevocadoRepository.EstaRevocadoAsync(jti))
        {
            return;
        }

        var exp = fechaExpiracion ?? DateTime.UtcNow.AddHours(8);
        await _tokenRevocadoRepository.RevocarAsync(jti, usuarioId, exp);
    }
}
