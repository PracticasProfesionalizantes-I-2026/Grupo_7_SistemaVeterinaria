using System.Security.Claims;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Reporte;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/reportes")]
[Authorize(Roles = "Administrador")]
public class ReportesController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReportesController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    /// <summary>
    /// Genera y persiste un nuevo reporte analítico consolidado (CU-11).
    /// </summary>
    [HttpPost("generar")]
    [HttpPost]
    public async Task<ActionResult<ReporteResponseDTO>> GenerarReporte([FromBody] GenerarReporteRequestDTO dto)
    {
        try
        {
            var usuarioIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var usuarioId = Guid.TryParse(usuarioIdClaim, out var id) ? id : Guid.Empty;
            var username = User.Identity?.Name ?? "admin";

            var result = await _reporteService.GenerarReporteAsync(dto, usuarioId, username);
            return CreatedAtAction(nameof(GetReporteById), new { id = result.Id }, result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (RangoFechasInvalidoException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Consulta el listado histórico de reportes generados (CU-10 Sub-variación 1).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReporteResumenDTO>>> GetAllReportes()
    {
        var result = await _reporteService.GetAllReportesAsync();
        return Ok(result);
    }

    /// <summary>
    /// Consulta el detalle de un reporte específico por su identificador (CU-10 Flujo Principal).
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReporteResponseDTO>> GetReporteById(Guid id)
    {
        try
        {
            var result = await _reporteService.GetReporteByIdAsync(id);
            return Ok(result);
        }
        catch (ReporteNotFoundException)
        {
            return NotFound(new { message = "El reporte solicitado no existe." });
        }
    }
}
