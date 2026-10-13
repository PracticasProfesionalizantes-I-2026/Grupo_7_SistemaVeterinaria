using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.DTOs.Usuario;
using Shared.Exceptions;

namespace API.Controllers;

[ApiController]
[Route("api/usuarios")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuariosController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UsuarioResponseDTO>>> GetAll(
        [FromQuery] string? username,
        [FromQuery] string? rol,
        [FromQuery] string? estado)
    {
        var result = await _usuarioService.GetAllAsync(username, rol, estado);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UsuarioResponseDTO>> GetById(Guid id)
    {
        try
        {
            var result = await _usuarioService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (UsuarioNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
