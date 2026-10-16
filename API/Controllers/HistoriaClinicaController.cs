using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.HistoriaClinica;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/historias-clinicas")]
[Authorize(Roles = "Veterinario,Administrador")]
public class HistoriaClinicaController : ControllerBase
{
    private readonly IHistoriaClinicaService _historiaClinicaService;

    public HistoriaClinicaController(IHistoriaClinicaService historiaClinicaService)
    {
        _historiaClinicaService = historiaClinicaService;
    }

    /// <summary>
    /// Consulta la historia clínica completa de una mascota (CU-03 Flujo Principal).
    /// </summary>
    [HttpGet("{mascotaId:guid}")]
    [HttpGet("mascota/{mascotaId:guid}")]
    [HttpGet("/api/mascotas/{mascotaId:guid}/historia-clinica")]
    public async Task<ActionResult<HistoriaClinicaResponseDTO>> GetHistoriaClinicaByMascotaId(Guid mascotaId)
    {
        try
        {
            var result = await _historiaClinicaService.GetHistoriaClinicaByMascotaIdAsync(mascotaId);
            return Ok(result);
        }
        catch (MascotaNotFoundException)
        {
            return NotFound(new { message = "No se encontró la historia clínica para la mascota solicitada." });
        }
    }

    /// <summary>
    /// Búsqueda previa de historias clínicas por criterio (nombre de mascota o DNI de dueño) - CU-03 Sub-variación 1.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<HistoriaClinicaBusquedaDTO>>> BuscarHistoriasClinicas([FromQuery] string? criterio)
    {
        var result = await _historiaClinicaService.BuscarHistoriasClinicasAsync(criterio);
        return Ok(result);
    }
}
