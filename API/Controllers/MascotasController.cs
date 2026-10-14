using System.Security.Claims;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Mascota;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/mascotas")]
[Authorize(Roles = "Recepcionista,Veterinario")]
public class MascotasController : ControllerBase
{
    private readonly IMascotaService _mascotaService;

    public MascotasController(IMascotaService mascotaService)
    {
        _mascotaService = mascotaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MascotaResponseDTO>>> GetAll([FromQuery] bool? soloActivas)
    {
        var result = await _mascotaService.GetAllAsync(soloActivas);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MascotaResponseDTO>> GetById(Guid id)
    {
        try
        {
            var result = await _mascotaService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("dueno/{duenoId:guid}")]
    public async Task<ActionResult<IEnumerable<MascotaResponseDTO>>> GetByDuenoId(Guid duenoId)
    {
        try
        {
            var result = await _mascotaService.GetByDuenoIdAsync(duenoId);
            return Ok(result);
        }
        catch (DuenoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<MascotaResponseDTO>> Create([FromBody] MascotaCreateDTO dto)
    {
        try
        {
            var result = await _mascotaService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DuenoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<MascotaResponseDTO>> Update(Guid id, [FromBody] MascotaUpdateDTO dto)
    {
        try
        {
            var result = await _mascotaService.UpdateAsync(id, dto);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    // MEJ-03: Baja lógica estandarizada con 200 OK y 409 Conflict si ya está inactiva
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<ActionResult<MascotaResponseDTO>> Desactivar(Guid id)
    {
        try
        {
            var result = await _mascotaService.DesactivarAsync(id);
            return Ok(result);
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (MascotaYaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // Ruta alternativa DELETE estandarizada como baja lógica (200 OK)
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<MascotaResponseDTO>> Delete(Guid id)
    {
        return await Desactivar(id);
    }

    // CU-07 Sub-variación 4: Reasignación de dueño (Cambiar dueño)
    [HttpPatch("{id:guid}/reasignar-dueno")]
    public async Task<ActionResult<MascotaResponseDTO>> ReasignarDueno(Guid id, [FromBody] ReasignarDuenoDTO dto)
    {
        var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var usuarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Usuario";

        if (string.IsNullOrEmpty(usuarioIdClaim) || !Guid.TryParse(usuarioIdClaim, out var usuarioId))
        {
            return Unauthorized(new { message = "Token inválido o reclamos de usuario ausentes." });
        }

        try
        {
            var result = await _mascotaService.ReasignarDuenoAsync(id, dto.NuevoDuenoId, usuarioId, usuarioUsername);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DuenoNotFoundException ex)
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
    }
}
