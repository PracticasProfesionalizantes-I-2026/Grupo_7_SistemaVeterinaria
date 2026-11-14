using System.Security.Claims;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Turno;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/turnos")]
[Authorize(Roles = "Recepcionista,Veterinario")]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _turnoService;

    public TurnosController(ITurnoService turnoService)
    {
        _turnoService = turnoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TurnoResponseDTO>>> GetAgenda([FromQuery] AgendaFilterDTO filtro)
    {
        try
        {
            var result = await _turnoService.GetAgendaAsync(filtro);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TurnoResponseDTO>> GetById(Guid id)
    {
        try
        {
            var result = await _turnoService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<TurnoResponseDTO>> Create([FromBody] TurnoCreateDTO dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.CreateManualAsync(dto, usuarioId, rolUsuario);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HorarioNoDisponibleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TurnoResponseDTO>> Update(Guid id, [FromBody] TurnoUpdateDTO dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        if (string.IsNullOrWhiteSpace(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.UpdateAsync(id, dto, usuarioId, rolUsuario);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoFinalizadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoYaCanceladoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HorarioNoDisponibleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<ActionResult<TurnoResponseDTO>> Confirmar(Guid id)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usuarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Usuario";
        var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.ConfirmarAsync(id, usuarioId, usuarioUsername, rolUsuario);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoNoPendienteException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HorarioNoDisponibleException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/iniciar-atencion")]
    [Authorize(Roles = "Veterinario")]
    public async Task<ActionResult<TurnoResponseDTO>> IniciarAtencion(Guid id)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usuarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Veterinario";

        if (string.IsNullOrWhiteSpace(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.IniciarAtencionAsync(id, usuarioId, usuarioUsername);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (MascotaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoNoConfirmadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoFinalizadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoYaCanceladoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/finalizar")]
    [Authorize(Roles = "Veterinario")]
    public async Task<ActionResult<TurnoResponseDTO>> Finalizar(Guid id)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usuarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Veterinario";

        if (string.IsNullOrWhiteSpace(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.FinalizarAsync(id, usuarioId, usuarioUsername);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (TurnoNoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoSinAtencionMedicaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoNoConfirmadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoFinalizadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoYaCanceladoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cancelar")]
    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<TurnoResponseDTO>> Cancelar(Guid id, [FromBody] TurnoCancelarDTO dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usuarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Usuario";
        var rolUsuario = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        if (string.IsNullOrEmpty(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId) || usuarioId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.CancelarAsync(id, dto, usuarioId, usuarioUsername, rolUsuario);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (TurnoVeterinarioNoAutorizadoException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (TurnoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TurnoEnAtencionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoFinalizadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoYaCanceladoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet("disponibilidad/{veterinarioId:guid}")]
    public async Task<ActionResult<IEnumerable<DisponibilidadVeterinarioDTO>>> GetDisponibilidad(Guid veterinarioId)
    {
        try
        {
            var result = await _turnoService.GetDisponibilidadVeterinarioAsync(veterinarioId);
            return Ok(result);
        }
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("disponibilidad/{veterinarioId:guid}")]
    [HttpPost("disponibilidad/{veterinarioId:guid}")]
    public async Task<ActionResult<IEnumerable<DisponibilidadVeterinarioDTO>>> ConfigurarDisponibilidad(
        Guid veterinarioId,
        [FromBody] ConfigurarDisponibilidadDTO dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioAutenticadoId) || usuarioAutenticadoId == Guid.Empty)
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        if (User.IsInRole("Veterinario") && !User.IsInRole("Recepcionista"))
        {
            if (usuarioAutenticadoId != veterinarioId || (dto.VeterinarioId != Guid.Empty && dto.VeterinarioId != usuarioAutenticadoId))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Un veterinario únicamente puede configurar su propia disponibilidad horaria." });
            }
        }

        try
        {
            var result = await _turnoService.ConfigurarDisponibilidadVeterinarioAsync(veterinarioId, dto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DisponibilidadInvalidaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
