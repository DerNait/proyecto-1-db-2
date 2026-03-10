using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Repositories;
using ProyectoRestaurantes.Api.Services;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantesController : ControllerBase
{
    private readonly RestauranteRepository _repo;
    private readonly GridFsService _gridFs;

    public RestaurantesController(RestauranteRepository repo, GridFsService gridFs)
    {
        _repo = repo;
        _gridFs = gridFs;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? busqueda,
        [FromQuery] string? categoria,
        [FromQuery] bool? activo,
        [FromQuery] string sortPor = "nombre",
        [FromQuery] int sortDir = 1,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20)
    {
        var (items, total) = await _repo.ObtenerAsync(busqueda, categoria, activo, sortPor, sortDir, skip, limit);
        return Ok(new { total, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(string id)
    {
        var restaurante = await _repo.ObtenerPorIdAsync(id);
        return restaurante is null ? NotFound() : Ok(restaurante);
    }

    [HttpGet("con-rating")]
    public async Task<IActionResult> ObtenerConRating([FromQuery] int limite = 10)
    {
        var resultado = await _repo.ObtenerConRatingAsync(limite);
        return Ok(resultado);
    }

    [HttpGet("categorias")]
    public async Task<IActionResult> ObtenerCategorias()
    {
        var categorias = await _repo.ObtenerCategoriasAsync();
        return Ok(categorias);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Restaurante restaurante)
    {
        var creado = await _repo.CrearAsync(restaurante);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(string id, [FromBody] Restaurante restaurante)
    {
        var ok = await _repo.ActualizarAsync(id, restaurante);
        return ok ? Ok(new { mensaje = "Restaurante actualizado." }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        var ok = await _repo.EliminarAsync(id);
        return ok ? Ok(new { mensaje = "Restaurante eliminado." }) : NotFound();
    }

    // Bulk: activar/desactivar varios restaurantes
    [HttpPut("bulk/activo")]
    public async Task<IActionResult> ActualizarActivosBulk([FromBody] BulkActivoRequest req)
    {
        var modificados = await _repo.ActualizarActivosBulkAsync(req.Ids, req.Activo);
        return Ok(new { mensaje = $"{modificados} restaurante(s) actualizados." });
    }

    // GridFS: subir imagen de portada
    [HttpPost("{id}/imagen")]
    public async Task<IActionResult> SubirImagen(string id, IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { error = "No se recibió ningún archivo." });

        using var stream = archivo.OpenReadStream();
        var gridFsId = await _gridFs.SubirArchivoAsync(stream, archivo.FileName, archivo.ContentType);
        await _repo.ActualizarImagenAsync(id, gridFsId);

        return Ok(new { gridFsId, mensaje = "Imagen subida y vinculada correctamente." });
    }
}

public record BulkActivoRequest(List<string> Ids, bool Activo);
