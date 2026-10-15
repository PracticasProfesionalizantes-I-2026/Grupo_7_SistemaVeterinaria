using System.Security.Claims;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.AtencionMedica;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/atenciones-medicas")]
[Route("api/atenciones")]
public class AtencionesMedicasController : ControllerBase
{
    private readonly IAtencionMedicaService _atencionMedicaService;

    public AtencionesMedicasController(IAtencionMedicaService atencionMedicaService)
    {
        _atencionMedicaService = atencionMedicaService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AtencionMedicaResponseDTO>>> GetAll()
    {
        var result = await _atencionMedicaService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AtencionMedicaResponseDTO>> GetById(Guid id)
    {
        try
        {
            var result = await _atencionMedicaService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (AtencionMedicaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("mascota/{mascotaId:guid}")]
    public async Task<ActionResult<IEnumerable<AtencionMedicaResponseDTO>>> GetByMascotaId(Guid mascotaId)
    {
        try
        {
            var result = await _atencionMedicaService.GetByMascotaIdAsync(mascotaId);
            return Ok(result);
        }
        catch (MascotaNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Veterinario")]
    public async Task<ActionResult<AtencionMedicaResponseDTO>> Create([FromBody] AtencionMedicaCreateDTO dto)
    {
        var veterinarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var veterinarioUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? User.Identity?.Name ?? "Veterinario";

        if (string.IsNullOrEmpty(veterinarioIdClaim) || !Guid.TryParse(veterinarioIdClaim, out var veterinarioId))
        {
            return Unauthorized(new { message = "Token inválido o reclamos de Veterinario ausentes." });
        }

        try
        {
            var result = await _atencionMedicaService.CreateAsync(dto, veterinarioId, veterinarioUsername);
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
        catch (MascotaInactivaException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (PacienteNoAptoVacunacionException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // RN-01: Inmutabilidad de las Atenciones Médicas (HTTP 409 Conflict)
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id)
    {
        try
        {
            await _atencionMedicaService.UpdateAsync(id);
            return Ok();
        }
        catch (AtencionInmutableException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // RN-01: Inmutabilidad de las Atenciones Médicas (HTTP 409 Conflict)
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _atencionMedicaService.DeleteAsync(id);
            return NoContent();
        }
        catch (AtencionInmutableException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
