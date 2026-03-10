using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Repositories;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportesController : ControllerBase
{
    private readonly ReportesRepository _reportesRepository;

    public ReportesController(ReportesRepository reportesRepository)
    {
        _reportesRepository = reportesRepository;
    }

    [HttpGet("top-restaurantes")]
    public async Task<IActionResult> GetTopRestaurantes([FromQuery] int limite = 5)
    {
        try
        {
            var reporte = await _reportesRepository.ObtenerTopRestaurantesAsync(limite);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al generar el reporte", detalle = ex.Message });
        }
    }

    [HttpGet("platillos-mas-vendidos")]
    public async Task<IActionResult> GetPlatillosMasVendidos([FromQuery] int limite = 5)
    {
        try
        {
            var reporte = await _reportesRepository.ObtenerPlatillosMasVendidosAsync(limite);
            return Ok(reporte);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { mensaje = "Error al generar el reporte", detalle = ex.Message });
        }
    }
}