using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Repositories;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ArticulosController : ControllerBase
{
    private readonly ArticuloRepository _repo;

    public ArticulosController(ArticuloRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? restauranteId,
        [FromQuery] string? busqueda,
        [FromQuery] bool? disponible,
        [FromQuery] string sortPor = "nombre",
        [FromQuery] int sortDir = 1,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 30)
    {
        var (items, total) = await _repo.ObtenerAsync(restauranteId, busqueda, disponible, sortPor, sortDir, skip, limit);
        return Ok(new { total, items });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(string id)
    {
        var articulo = await _repo.ObtenerPorIdAsync(id);
        return articulo is null ? NotFound() : Ok(articulo);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Articulo articulo)
    {
        var creado = await _repo.CrearAsync(articulo);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(string id, [FromBody] Articulo articulo)
    {
        var ok = await _repo.ActualizarAsync(id, articulo);
        return ok ? Ok(new { mensaje = "Artículo actualizado." }) : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        var ok = await _repo.EliminarAsync(id);
        return ok ? Ok(new { mensaje = "Artículo eliminado." }) : NotFound();
    }

    // Manejo de arrays: ingredientes
    [HttpPost("{id}/ingredientes")]
    public async Task<IActionResult> AgregarIngrediente(string id, [FromBody] TagRequest req)
    {
        var ok = await _repo.AgregarIngredienteAsync(id, req.Valor);
        return ok ? Ok(new { mensaje = "Ingrediente agregado." }) : NotFound();
    }

    [HttpDelete("{id}/ingredientes")]
    public async Task<IActionResult> QuitarIngrediente(string id, [FromBody] TagRequest req)
    {
        var ok = await _repo.QuitarIngredienteAsync(id, req.Valor);
        return ok ? Ok(new { mensaje = "Ingrediente removido." }) : NotFound();
    }

    // Manejo de arrays: categorías (addToSet)
    [HttpPost("{id}/categorias")]
    public async Task<IActionResult> AgregarCategoria(string id, [FromBody] TagRequest req)
    {
        var ok = await _repo.AgregarCategoriaAsync(id, req.Valor);
        return ok ? Ok(new { mensaje = "Categoría agregada." }) : NotFound();
    }

    [HttpDelete("{id}/categorias")]
    public async Task<IActionResult> QuitarCategoria(string id, [FromBody] TagRequest req)
    {
        var ok = await _repo.QuitarCategoriaAsync(id, req.Valor);
        return ok ? Ok(new { mensaje = "Categoría removida." }) : NotFound();
    }

    // Bulk Write: actualizar precios masivamente
    [HttpPut("bulk/precios")]
    public async Task<IActionResult> ActualizarPreciosBulk([FromBody] List<PrecioUpdate> cambios)
    {
        var lista = cambios.Select(c => (c.Id, c.NuevoPrecio)).ToList();
        var result = await _repo.ActualizarPreciosBulkAsync(lista);
        return Ok(new
        {
            modificados = result.ModifiedCount,
            procesados = result.ProcessedRequests.Count
        });
    }
}

public record TagRequest(string Valor);
public record PrecioUpdate(string Id, double NuevoPrecio);
