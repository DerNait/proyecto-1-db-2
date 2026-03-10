using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Repositories;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuariosController : ControllerBase
{
    private readonly UsuarioRepository _repo;

    public UsuariosController(UsuarioRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20)
    {
        var (items, total) = await _repo.ObtenerAsync(busqueda, skip, limit);
        return Ok(new { total, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(string id)
    {
        var usuario = await _repo.ObtenerPorIdAsync(id);
        return usuario is null ? NotFound() : Ok(usuario);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(string id, [FromBody] ActualizarUsuarioRequest req)
    {
        var ok = await _repo.ActualizarAsync(id, req.Nombre, req.Roles);
        return ok ? Ok(new { mensaje = "Usuario actualizado." }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        var ok = await _repo.EliminarAsync(id);
        return ok ? Ok(new { mensaje = "Usuario eliminado." }) : NotFound();
    }

    // Documentos embebidos: gestión de direcciones
    [HttpPost("{id}/direcciones")]
    public async Task<IActionResult> AgregarDireccion(string id, [FromBody] Direccion direccion)
    {
        var ok = await _repo.AgregarDireccionAsync(id, direccion);
        return ok ? Ok(new { mensaje = "Dirección agregada." }) : NotFound();
    }

    [HttpDelete("{id}/direcciones/{alias}")]
    public async Task<IActionResult> QuitarDireccion(string id, string alias)
    {
        var ok = await _repo.QuitarDireccionAsync(id, alias);
        return ok ? Ok(new { mensaje = "Dirección removida." }) : NotFound();
    }
}

public record ActualizarUsuarioRequest(string Nombre, List<string> Roles);
