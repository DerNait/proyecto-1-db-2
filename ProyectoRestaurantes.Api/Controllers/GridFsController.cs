using Microsoft.AspNetCore.Mvc;
using ProyectoRestaurantes.Api.Services;

namespace ProyectoRestaurantes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GridFsController : ControllerBase
{
    private readonly GridFsService _gridFs;

    public GridFsController(GridFsService gridFs) => _gridFs = gridFs;

    [HttpGet]
    public async Task<IActionResult> ListarArchivos([FromQuery] int skip = 0, [FromQuery] int limit = 50)
    {
        var archivos = await _gridFs.ListarArchivosAsync(skip, limit);
        return Ok(archivos);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> SubirArchivo(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
            return BadRequest(new { error = "No se recibió ningún archivo." });

        using var stream = archivo.OpenReadStream();
        var id = await _gridFs.SubirArchivoAsync(stream, archivo.FileName, archivo.ContentType);
        return Ok(new { id, nombre = archivo.FileName, mensaje = "Archivo subido exitosamente." });
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DescargarArchivo(string id)
    {
        try
        {
            var (stream, fileName, contentType) = await _gridFs.DescargarArchivoAsync(id);
            return File(stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> EliminarArchivo(string id)
    {
        try
        {
            await _gridFs.EliminarArchivoAsync(id);
            return Ok(new { mensaje = "Archivo eliminado de GridFS." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
