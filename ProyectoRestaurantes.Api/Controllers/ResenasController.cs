using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Models;
using ProyectoRestaurantes.Api.Repositories;
using ProyectoRestaurantes.Api.Services;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResenasController : ControllerBase
{
    private readonly ResenaRepository _repo;
    private readonly ResenaService _resenaService;

    public ResenasController(ResenaRepository repo, ResenaService resenaService)
    {
        _repo = repo;
        _resenaService = resenaService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? restauranteId,
        [FromQuery] string? usuarioId,
        [FromQuery] int? calificacionMin,
        [FromQuery] string sortPor = "fecha",
        [FromQuery] int sortDir = -1,
        [FromQuery] int skip = 0,
        [FromQuery] int limit = 20)
    {
        var (items, total) = await _repo.ObtenerAsync(restauranteId, usuarioId, calificacionMin, sortPor, sortDir, skip, limit);
        var resultItems = items.Select(d => new {
            _id = d.GetValue("_id", "").ToString(),
            usuario_id = d.GetValue("usuario_id", "").ToString(),
            restaurante_id = d.GetValue("restaurante_id", "").ToString(),
            calificacion = d.GetValue("calificacion", 0).ToInt32(),
            comentario = d.GetValue("comentario", "").ToString(),
            fecha = d.GetValue("fecha", DateTime.UtcNow).ToUniversalTime(),
            usuario = d.Contains("usuario") && d["usuario"].IsBsonDocument ? new {
                nombre = d["usuario"].AsBsonDocument.GetValue("nombre", "").ToString(),
                email = d["usuario"].AsBsonDocument.GetValue("email", "").ToString()
            } : null,
            restaurante = d.Contains("restaurante") && d["restaurante"].IsBsonDocument ? new {
                nombre = d["restaurante"].AsBsonDocument.GetValue("nombre", "").ToString()
            } : null
        });
        return Ok(new { total, items = resultItems });
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] Resena resena)
    {
        try
        {
            if (resena.Calificacion < 1 || resena.Calificacion > 5)
                return BadRequest(new { error = "La calificación debe ser entre 1 y 5." });

            var creada = await _resenaService.CrearResenaTransaccionalAsync(resena);
            return Ok(creada);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id)
    {
        var ok = await _repo.EliminarAsync(id);
        return ok ? Ok(new { mensaje = "Reseña eliminada." }) : NotFound();
    }

    [HttpGet("estadisticas")]
    public async Task<IActionResult> ObtenerEstadisticas()
    {
        var stats = await _repo.ObtenerEstadisticasPorRestauranteAsync();
        var resultStats = stats.Select(d => new {
            _id = d.GetValue("_id", "").ToString(),
            totalResenas = d.GetValue("totalResenas", 0).ToInt32(),
            promedioCalificacion = d.GetValue("promedioCalificacion", 0).ToDouble(),
            minCalificacion = d.GetValue("minCalificacion", 0).ToInt32(),
            maxCalificacion = d.GetValue("maxCalificacion", 0).ToInt32(),
            restaurante = d.Contains("restaurante") && d["restaurante"].IsBsonDocument ? new {
                nombre = d["restaurante"].AsBsonDocument.GetValue("nombre", "").ToString()
            } : null
        });
        return Ok(resultStats);
    }
}
