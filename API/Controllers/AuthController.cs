using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Auth;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO dto)
    {
        try
        {
            var result = await _authService.LoginAsync(dto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (UsuarioBloqueadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (UsuarioInactivoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [HttpPost("cambiar-password-temporal")]
    public async Task<ActionResult<LoginResponseDTO>> CambiarPasswordTemporal([FromBody] CambiarPasswordTemporalDTO dto)
    {
        try
        {
            var result = await _authService.CambiarPasswordTemporalAsync(dto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CredencialesInvalidasException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (UsuarioBloqueadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (UsuarioInactivoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(jti) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Token inválido o reclamos ausentes." });
        }

        DateTime? exp = null;
        var expClaim = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (long.TryParse(expClaim, out var expSeconds))
        {
            exp = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
        }

        await _authService.LogoutAsync(jti, userId, exp);
        return Ok(new { mensaje = "Sesión cerrada exitosamente." });
    }

    [Authorize]
    [HttpGet("verificar-sesion")]
    public IActionResult VerificarSesion()
    {
        return Ok(new { mensaje = "Sesión activa." });
    }
}
