using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Dueno;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/duenos")]
public class DuenosController : ControllerBase
{
    private readonly IDuenoService _duenoService;

    public DuenosController(IDuenoService duenoService)
    {
        _duenoService = duenoService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DuenoResponseDTO>>> GetAll()
    {
        var result = await _duenoService.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DuenoResponseDTO>> GetById(Guid id)
    {
        try
        {
            var result = await _duenoService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (DuenoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<DuenoResponseDTO>> Create([FromBody] DuenoCreateDTO dto)
    {
        try
        {
            var result = await _duenoService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DniDuplicadoException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DuenoResponseDTO>> Update(Guid id, [FromBody] DuenoUpdateDTO dto)
    {
        try
        {
            var result = await _duenoService.UpdateAsync(id, dto);
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
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _duenoService.DeleteAsync(id);
            return NoContent();
        }
        catch (DuenoNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (DuenoConMascotasException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
