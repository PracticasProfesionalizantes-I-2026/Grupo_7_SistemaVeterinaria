using BusinessLogic.Interfaces;
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
}
