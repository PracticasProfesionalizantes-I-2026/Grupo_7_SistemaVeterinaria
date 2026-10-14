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
        var result = await _turnoService.GetAgendaAsync(filtro);
        return Ok(result);
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
        try
        {
            var result = await _turnoService.CreateManualAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        try
        {
            var result = await _turnoService.UpdateAsync(id, dto);
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
        catch (VeterinarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (TurnoFinalizadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (TurnoYaCanceladoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (HorarioNoDisponibleException ex)
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

        if (string.IsNullOrEmpty(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _turnoService.CancelarAsync(id, dto, usuarioId, usuarioUsername);
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
